using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BlockGame.util.xNBT;

/**
 * Primitives: 123b (byte), 123s (short), 123 (int), 123L (long), 3.14f (float), 3.14d (double)
 * Unsigned: 123ub, 123us, 123u, 123uL
 * Strings: "text" with \" and \\ escaping
 * Compounds: {name1: value1, name2: value2}
 * Lists: [1, 2, 3] or [TAG_Type;] for empty lists (type preserved!)
 * Arrays: [B; 1,2,3] for byte[], [I; ...] for int[], etc.
 */
public static class SNBT {
    public static string toString(NBTTag tag, bool prettyPrint = false) {
        // root tag can't have a name if compound
        if (tag is NBTCompound compound && !string.IsNullOrEmpty(compound.name)) {
            throw new ArgumentException("Root tag must not have a name!", nameof(tag));
        }

        var writer = new SNBTWriter(prettyPrint);
        return writer.write(tag);
    }

    public static NBTTag parse(string snbt) {
        var parser = new SNBTParser(snbt);
        var nbt = parser.parse();
        // root tag can't have a name if compound
        if (nbt is NBTCompound compound && !string.IsNullOrEmpty(compound.name)) {
            throw new ArgumentException("Root tag must not have a name!", nameof(snbt));
        }

        return nbt;
    }

    public static void writeToFile(NBTTag tag, string path, bool prettyPrint = false) {
        var s = toString(tag, prettyPrint);
        File.WriteAllText(path, s);
    }

    public static NBTTag readFromFile(string path) {
        if (!File.Exists(path)) {
            throw new FileNotFoundException($"File not found: {path}");
        }

        var content = File.ReadAllText(path);
        return parse(content);
    }
}

internal partial class SNBTWriter {
    private readonly StringBuilder sb = new();
    private readonly bool prettyPrint;
    private int indentLevel;
    private const string INDENT = "  ";
    
    // Regex for valid unquoted strings
    private static readonly Regex UnquotedPattern = unquoted();
    private static readonly HashSet<string> ReservedSuffixes = ["b", "ub", "s", "us", "u", "L", "uL", "f", "d", "END"];

    public SNBTWriter(bool prettyPrint = false) {
        this.prettyPrint = prettyPrint;
        this.indentLevel = 0;
    }

    public string write(NBTTag tag) {
        writeValue(tag);
        return sb.ToString();
    }

    private void writeIndent() {
        if (!prettyPrint) return;
        for (int i = 0; i < indentLevel; i++) {
            sb.Append(INDENT);
        }
    }

    private void writeNewLine() {
        if (prettyPrint) {
            sb.AppendLine();
        }
    }

    private bool canBeUnquoted(string s) {
        if (string.IsNullOrEmpty(s)) return false;
        if (ReservedSuffixes.Contains(s)) return false;
        return UnquotedPattern.IsMatch(s);
    }

    private void writeValue(NBTTag tag) {
        switch (tag) {
            case NBTEnd:
                sb.Append("END");
                break;
            case NBTByte t:
                sb.Append(t.data).Append('b');
                break;
            case NBTShort t:
                sb.Append(t.data).Append('s');
                break;
            case NBTUShort t:
                sb.Append(t.data).Append("us");
                break;
            case NBTInt t:
                sb.Append(t.data);
                break;
            case NBTUInt t:
                sb.Append(t.data).Append('u');
                break;
            case NBTLong t:
                sb.Append(t.data).Append('L');
                break;
            case NBTULong t:
                sb.Append(t.data).Append("uL");
                break;
            case NBTFloat t:
                sb.Append(t.data.ToString(CultureInfo.InvariantCulture)).Append('f');
                break;
            case NBTDouble t:
                sb.Append(t.data.ToString(CultureInfo.InvariantCulture)).Append('d');
                break;
            case NBTString t:
                writeString(t.data);
                break;
            case NBTCompound t:
                writeCompound(t);
                break;
            case INBTList list and NBTList<NBTTag> genericList:
                writeList(genericList.list, list.listType);
                break;
            case NBTByteArray t:
                writeArray("B", t, t.data);
                break;
            case NBTShortArray t:
                writeArray("S", t, t.data);
                break;
            case NBTUShortArray t:
                writeArray("US", t, t.data);
                break;
            case NBTIntArray t:
                writeArray("I", t, t.data);
                break;
            case NBTUIntArray t:
                writeArray("UI", t, t.data);
                break;
            case NBTLongArray t:
                writeArray("L", t, t.data);
                break;
            case NBTULongArray t:
                writeArray("UL", t, t.data);
                break;
            default:
                // Handle typed lists
                if (tag is INBTList listTag) {
                    var listProp = tag.GetType().GetField("list");
                    var list = listProp?.GetValue(tag);
                    if (list is System.Collections.IList items) {
                        var tagList = new List<NBTTag>();
                        foreach (var item in items) {
                            tagList.Add((NBTTag)item);
                        }

                        writeList(tagList, listTag.listType);
                    }
                }
                else {
                    throw new ArgumentException($"Unsupported NBTTag type: {tag.GetType().Name}", nameof(tag));
                }

                break;
        }
    }

    private void writeString(string s) {
        if (canBeUnquoted(s)) {
            sb.Append(s);
        } else {
            sb.Append('"');
            foreach (char c in s) {
                if (c == '"') sb.Append("\\\"");
                else if (c == '\\') sb.Append(@"\\");
                else sb.Append(c);
            }
            sb.Append('"');
        }
    }

    private void writeCompound(NBTCompound compound) {
        sb.Append('{');
        if (compound.dict.Count > 0 && prettyPrint) {
            writeNewLine();
            indentLevel++;
        }
        
        bool first = true;
        foreach (var kvp in compound.dict) {
            if (!first) {
                sb.Append(',');
                if (prettyPrint) {
                    writeNewLine();
                } else {
                    sb.Append(' ');
                }
            }
            first = false;
            
            if (prettyPrint) writeIndent();
            
            // Write key (prefer unquoted if possible)
            if (canBeUnquoted(kvp.Key)) {
                sb.Append(kvp.Key);
            } else {
                sb.Append('"');
                foreach (char c in kvp.Key) {
                    if (c == '"') sb.Append("\\\"");
                    else if (c == '\\') sb.Append(@"\\");
                    else sb.Append(c);
                }
                sb.Append('"');
            }
            
            sb.Append(": ");
            writeValue(kvp.Value);
        }

        if (compound.dict.Count > 0 && prettyPrint) {
            indentLevel--;
            writeNewLine();
            writeIndent();
        }
        sb.Append('}');
    }

    private void writeList(List<NBTTag> list, NBTType listType) {
        sb.Append('[');
        
        if (list.Count == 0) {
            sb.Append(NBTTag.getTypeName(listType)).Append(';');
        }
        else {
            if (prettyPrint && (list.Any(t => t is NBTCompound || t is INBTList))) {
                writeNewLine();
                indentLevel++;
            }
            
            for (int i = 0; i < list.Count; i++) {
                if (i > 0) {
                    sb.Append(',');
                    if (prettyPrint) {
                        if (list[i] is NBTCompound || list[i] is INBTList) {
                            writeNewLine();
                        } else {
                            sb.Append(' ');
                        }
                    } else {
                        sb.Append(' ');
                    }
                }
                
                if (prettyPrint && (list[i] is NBTCompound || list[i] is INBTList)) {
                    writeIndent();
                }
                
                writeValue(list[i]);
            }
            
            if (prettyPrint && (list.Any(t => t is NBTCompound || t is INBTList))) {
                indentLevel--;
                writeNewLine();
                writeIndent();
            }
        }

        sb.Append(']');
    }

    private void writeArray<T>(string prefix, NBTTag t, T[] data) {
        sb.Append('[').Append(prefix).Append("; ");
        
        for (int i = 0; i < data.Length; i++) {
            if (i > 0) sb.Append(", ");
            sb.Append(data[i]?.ToString() ?? "0");
        }

        sb.Append(']');
    }

    [GeneratedRegex(@"^[a-zA-Z_][a-zA-Z0-9_\.\+\-]*$")]
    private static partial Regex unquoted();
}

internal class SNBTParser {
    private readonly string input;
    private int pos;

    public SNBTParser(string input) {
        this.input = input;
        this.pos = 0;
    }

    public NBTTag parse() {
        skipWhitespace();
        var result = parseValue(null);
        skipWhitespace();
        if (pos < input.Length) {
            throw new FormatException($"Unexpected character at position {pos}: {input[pos]}");
        }

        return result;
    }

    private void skipWhitespace() {
        while (pos < input.Length && char.IsWhiteSpace(input[pos])) {
            pos++;
        }
    }

    private char peek() {
        return pos < input.Length ? input[pos] : '\0';
    }

    private char next() {
        return pos < input.Length ? input[pos++] : '\0';
    }

    private NBTTag parseValue(string? name) {
        skipWhitespace();
        char c = peek();

        if (c == '{') {
            return parseCompound(name);
        }
        else if (c == '[') {
            return parseListOrArray(name);
        }
        else if (c == '"') {
            return new NBTString(name, parseQuotedString());
        }
        else if (c == '-' || char.IsDigit(c)) {
            return parseNumber(name);
        }
        else if (input.Length - pos >= 3 && input.Substring(pos, 3) == "END") {
            pos += 3;
            return new NBTEnd();
        }
        else if (char.IsLetter(c) || c == '_') {
            // Could be unquoted string
            return parseUnquotedStringOrNumber(name);
        }
        else {
            throw new FormatException($"Unexpected character at position {pos}: {c}");
        }
    }

    private NBTCompound parseCompound(string? name) {
        next(); // consume '{'
        var compound = new NBTCompound(name);
        skipWhitespace();

        while (peek() != '}') {
            string key = parseKey();
            skipWhitespace();
            if (next() != ':') {
                throw new FormatException($"Expected ':' at position {pos - 1}");
            }

            skipWhitespace();
            var value = parseValue(key);
            compound.add(value);

            skipWhitespace();
            if (peek() == ',') {
                next();
                skipWhitespace();
            }
            else if (peek() != '}') {
                throw new FormatException($"Expected ',' or '}}' at position {pos}");
            }
        }

        next(); // consume '}'
        return compound;
    }

    private string parseKey() {
        skipWhitespace();
        if (peek() == '"') {
            return parseQuotedString();
        } else {
            return parseUnquotedKey();
        }
    }

    private string parseUnquotedKey() {
        int start = pos;
        while (pos < input.Length) {
            char c = input[pos];
            if (char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '+' || c == '-') {
                pos++;
            } else {
                break;
            }
        }
        
        if (pos == start) {
            throw new FormatException($"Expected key at position {pos}");
        }
        
        return input.Substring(start, pos - start);
    }

    private NBTTag parseUnquotedStringOrNumber(string? name) {
        int start = pos;
        
        // Collect the unquoted token
        while (pos < input.Length) {
            char c = input[pos];
            if (char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '+' || c == '-') {
                pos++;
            } else {
                break;
            }
        }
        
        string token = input.Substring(start, pos - start);
        
        // Check for number suffixes
        if (token.EndsWith('b') && tryParseNumber(token[..^1], out var b)) {
            return new NBTByte(name, byte.Parse(token[..^1]));
        }
        if (token.EndsWith("ub") && tryParseNumber(token[..^2], out var ub)) {
            return new NBTByte(name, byte.Parse(token[..^2]));
        }
        if (token.EndsWith('s') && tryParseNumber(token[..^1], out var s)) {
            return new NBTShort(name, short.Parse(token[..^1]));
        }
        if (token.EndsWith("us") && tryParseNumber(token[..^2], out var us)) {
            return new NBTUShort(name, ushort.Parse(token[..^2]));
        }
        if (token.EndsWith('u') && tryParseNumber(token[..^1], out var u)) {
            return new NBTUInt(name, uint.Parse(token[..^1]));
        }
        if (token.EndsWith('L') && tryParseNumber(token[..^1], out var l)) {
            return new NBTLong(name, long.Parse(token[..^1]));
        }
        if (token.EndsWith("uL") && tryParseNumber(token[..^2], out var ul)) {
            return new NBTULong(name, ulong.Parse(token[..^2]));
        }
        if (token.EndsWith('f') && tryParseFloat(token[..^1], out var f)) {
            return new NBTFloat(name, float.Parse(token[..^1], CultureInfo.InvariantCulture));
        }
        if (token.EndsWith('d') && tryParseFloat(token[..^1], out var d)) {
            return new NBTDouble(name, double.Parse(token[..^1], CultureInfo.InvariantCulture));
        }
        
        // Not a number, treat as unquoted string
        return new NBTString(name, token);
    }

    private bool tryParseNumber(string s, out object _) {
        _ = null;
        return !string.IsNullOrEmpty(s) && s.All(c => char.IsDigit(c) || c == '-');
    }

    private bool tryParseFloat(string s, out object _) {
        _ = null;
        if (string.IsNullOrEmpty(s)) return false;
        int dotCount = 0;
        for (int i = 0; i < s.Length; i++) {
            char c = s[i];
            if (c == '.') {
                dotCount++;
                if (dotCount > 1) return false;
            } else if (c == '-') {
                if (i != 0) return false;
            } else if (!char.IsDigit(c)) {
                return false;
            }
        }
        return true;
    }

    private NBTTag parseListOrArray(string? name) {
        next(); // consume '['
        skipWhitespace();

        // Check for array prefix or empty list type
        int savedPos = pos;
        string? prefix = tryParseArrayPrefix();

        if (prefix != null) {
            // It's an array
            return parseArray(name, prefix);
        }

        // Check for empty list with type
        pos = savedPos;
        var emptyListType = tryParseEmptyListType();
        if (emptyListType != null) {
            return NBTTag.createListTag(emptyListType.Value, name);
        }

        // Regular list
        pos = savedPos;
        return parseList(name);
    }

    private string? tryParseArrayPrefix() {
        int start = pos;
        while (pos < input.Length && char.IsLetter(input[pos])) {
            pos++;
        }

        if (pos > start && pos < input.Length && input[pos] == ';') {
            string prefix = input.Substring(start, pos - start);
            if (isValidArrayPrefix(prefix)) {
                pos++; // consume ';'
                return prefix;
            }
        }

        pos = start;
        return null;
    }

    private NBTType? tryParseEmptyListType() {
        int start = pos;
        while (pos < input.Length && (char.IsLetter(input[pos]) || input[pos] == '_')) {
            pos++;
        }

        if (pos > start && pos < input.Length && input[pos] == ';') {
            string typeName = input.Substring(start, pos - start);
            var type = parseTypeName(typeName);
            if (type != null) {
                pos++; // consume ';'
                skipWhitespace();
                if (peek() == ']') {
                    pos++; // consume ']'
                    return type;
                }
            }
        }

        pos = start;
        return null;
    }

    private bool isValidArrayPrefix(string prefix) {
        return prefix switch {
            "B" or "S" or "US" or "I" or "UI" or "L" or "UL" => true,
            _ => false
        };
    }

    private NBTType? parseTypeName(string name) {
        foreach (NBTType type in Enum.GetValues<NBTType>()) {
            if (NBTTag.getTypeName(type) == name) {
                return type;
            }
        }

        return null;
    }

    private static NBTTag createEmptyList(NBTType listType, string? name) {
        return listType switch {
            NBTType.TAG_End => new NBTList<NBTEnd>(listType, name),
            NBTType.TAG_Byte => new NBTList<NBTByte>(listType, name),
            NBTType.TAG_Short => new NBTList<NBTShort>(listType, name),
            NBTType.TAG_UShort => new NBTList<NBTUShort>(listType, name),
            NBTType.TAG_Int => new NBTList<NBTInt>(listType, name),
            NBTType.TAG_UInt => new NBTList<NBTUInt>(listType, name),
            NBTType.TAG_Long => new NBTList<NBTLong>(listType, name),
            NBTType.TAG_ULong => new NBTList<NBTULong>(listType, name),
            NBTType.TAG_Float => new NBTList<NBTFloat>(listType, name),
            NBTType.TAG_Double => new NBTList<NBTDouble>(listType, name),
            NBTType.TAG_String => new NBTList<NBTString>(listType, name),
            NBTType.TAG_List => new NBTList<NBTList<NBTTag>>(listType, name),
            NBTType.TAG_Compound => new NBTList<NBTCompound>(listType, name),
            NBTType.TAG_Byte_Array => new NBTList<NBTByteArray>(listType, name),
            NBTType.TAG_Short_Array => new NBTList<NBTShortArray>(listType, name),
            NBTType.TAG_UShort_Array => new NBTList<NBTUShortArray>(listType, name),
            NBTType.TAG_Int_Array => new NBTList<NBTIntArray>(listType, name),
            NBTType.TAG_UInt_Array => new NBTList<NBTUIntArray>(listType, name),
            NBTType.TAG_Long_Array => new NBTList<NBTLongArray>(listType, name),
            NBTType.TAG_ULong_Array => new NBTList<NBTULongArray>(listType, name),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private NBTTag parseArray(string? name, string prefix) {
        skipWhitespace();
        var values = new List<string>();

        while (peek() != ']') {
            values.Add(parseArrayValue());
            skipWhitespace();
            if (peek() == ',') {
                next();
                skipWhitespace();
            }
            else if (peek() != ']') {
                throw new FormatException($"Expected ',' or ']' at position {pos}");
            }
        }

        next(); // consume ']'

        return prefix switch {
            "B" => new NBTByteArray(name, Array.ConvertAll(values.ToArray(), byte.Parse)),
            "S" => new NBTShortArray(name, Array.ConvertAll(values.ToArray(), short.Parse)),
            "US" => new NBTUShortArray(name, Array.ConvertAll(values.ToArray(), ushort.Parse)),
            "I" => new NBTIntArray(name, Array.ConvertAll(values.ToArray(), int.Parse)),
            "UI" => new NBTUIntArray(name, Array.ConvertAll(values.ToArray(), uint.Parse)),
            "L" => new NBTLongArray(name, Array.ConvertAll(values.ToArray(), long.Parse)),
            "UL" => new NBTULongArray(name, Array.ConvertAll(values.ToArray(), ulong.Parse)),
            _ => throw new FormatException($"Unknown array type: {prefix}")
        };
    }

    private string parseArrayValue() {
        int start = pos;
        while (pos < input.Length && input[pos] != ',' && input[pos] != ']' && !char.IsWhiteSpace(input[pos])) {
            pos++;
        }

        return input.Substring(start, pos - start);
    }

    private NBTTag parseList(string? name) {
        var items = new List<NBTTag>();

        while (peek() != ']') {
            items.Add(parseValue(null));
            skipWhitespace();
            if (peek() == ',') {
                next();
                skipWhitespace();
            }
            else if (peek() != ']') {
                throw new FormatException($"Expected ',' or ']' at position {pos}");
            }
        }

        next(); // consume ']'

        if (items.Count == 0) {
            // Plain empty list [] without type specification defaults to TAG_End
            return new NBTList<NBTEnd>(NBTType.TAG_End, name);
        }

        // Infer list type from first element
        NBTType listType = items[0].id;
        var list = NBTTag.createListTag(listType, name);

        // Add items using reflection (ugly but necessary due to generic constraints)
        // TODO actually fix this lol
        var listProp = list.GetType().GetField("list");
        var actualList = listProp?.GetValue(list);
        if (actualList is System.Collections.IList ilist) {
            foreach (var item in items) {
                if (item.id != listType) {
                    throw new FormatException($"Mixed types in list: expected {listType}, got {item.id}");
                }

                ilist.Add(item);
            }
        }

        return list;
    }

    private string parseQuotedString() {
        if (next() != '"') {
            throw new FormatException($"Expected '\"' at position {pos - 1}");
        }

        var sb = new StringBuilder();
        while (true) {
            char c = next();
            if (c == '\0') {
                throw new FormatException("Unterminated string");
            }
            else if (c == '"') {
                break;
            }
            else if (c == '\\') {
                char escaped = next();
                if (escaped == '"') sb.Append('"');
                else if (escaped == '\\') sb.Append('\\');
                else throw new FormatException($"Invalid escape sequence at position {pos - 1}");
            }
            else {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private NBTTag parseNumber(string? name) {
        int start = pos;
        bool negative = false;
        bool isFloat = false;

        if (peek() == '-') {
            negative = true;
            next();
        }

        while (pos < input.Length) {
            char c = input[pos];
            if (char.IsDigit(c)) {
                pos++;
            }
            else if (c == '.' && !isFloat) {
                isFloat = true;
                pos++;
            }
            else {
                break;
            }
        }

        string numStr = input.Substring(start, pos - start);

        // Check for suffix
        string suffix = "";
        int suffixStart = pos;
        while (pos < input.Length && char.IsLetter(input[pos])) {
            pos++;
        }

        if (pos > suffixStart) {
            suffix = input.Substring(suffixStart, pos - suffixStart);
        }

        // Parse based on suffix
        return suffix switch {
            "b" => new NBTByte(name, byte.Parse(numStr)),
            "ub" => new NBTByte(name, byte.Parse(numStr)),
            "s" => new NBTShort(name, short.Parse(numStr)),
            "us" => new NBTUShort(name, ushort.Parse(numStr)),
            "u" => new NBTUInt(name, uint.Parse(numStr)),
            "L" => new NBTLong(name, long.Parse(numStr)),
            "uL" => new NBTULong(name, ulong.Parse(numStr)),
            "f" => new NBTFloat(name, float.Parse(numStr, CultureInfo.InvariantCulture)),
            "d" => new NBTDouble(name, double.Parse(numStr, CultureInfo.InvariantCulture)),
            "" when isFloat => new NBTDouble(name, double.Parse(numStr, CultureInfo.InvariantCulture)),
            "" => new NBTInt(name, int.Parse(numStr)),
            _ => throw new FormatException($"Unknown number suffix: {suffix}")
        };
    }
}