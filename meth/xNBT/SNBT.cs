using System.Collections;
using System.Globalization;
using System.Text;

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

public class SNBTWriter {
    private readonly StringBuilder sb = new();
    private readonly bool prettyPrint;
    private int indentLevel;
    private const string INDENT = "  ";
    
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

    private static bool canBeUnquoted(string s) {
        if (string.IsNullOrEmpty(s)) return false;
        if (ReservedSuffixes.Contains(s)) return false;
        
        // First char must be letter or underscore
        char first = s[0];
        if (!char.IsLetter(first) && first != '_') return false;
        
        // Rest can be alphanumeric, underscore, dot, plus, minus
        for (int i = 1; i < s.Length; i++) {
            char c = s[i];
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '.' && c != '+' && c != '-') {
                return false;
            }
        }
        
        return true;
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
            case INBTList list:
                writeListInterface(list, tag);
                break;
            case NBTByteArray t:
                writeArray("B", t.data);
                break;
            case NBTShortArray t:
                writeArray("S", t.data);
                break;
            case NBTUShortArray t:
                writeArray("US", t.data);
                break;
            case NBTIntArray t:
                writeArray("I", t.data);
                break;
            case NBTUIntArray t:
                writeArray("UI", t.data);
                break;
            case NBTLongArray t:
                writeArray("L", t.data);
                break;
            case NBTULongArray t:
                writeArray("UL", t.data);
                break;
            case NBTStruct t:
                writeStruct(t);
                break;
            default:
                throw new ArgumentException($"Unsupported NBTTag type: {tag.GetType().Name}", nameof(tag));
        }
    }

    private void writeStruct(NBTStruct s) {
        sb.Append("0x");
        foreach (byte b in s.data) {
            sb.Append(b.ToString("X2"));
        }
    }

    private void writeListInterface(INBTList list, NBTTag tag) {
        // Extract the list items via reflection (necessary due to generic constraints)
        var listField = tag.GetType().GetField("list");
        if (listField?.GetValue(tag) is IList items) {
            var tagList = new List<NBTTag>(items.Count);
            foreach (var item in items) {
                tagList.Add((NBTTag)item);
            }
            writeList(tagList, list.listType);
        } else {
            // Empty list
            writeList(new List<NBTTag>(), list.listType);
        }
    }

    private void writeString(string s) {
        if (canBeUnquoted(s)) {
            sb.Append(s);
        } else {
            sb.Append('"');
            foreach (char c in s) {
                switch (c) {
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append(@"\\");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
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
        foreach (var kvp in compound.dict.Pairs) {
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
            
            // Write key
            writeString(kvp.Key);
            
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
            bool complexItems = list.Any(t => t is NBTCompound or INBTList);
            if (prettyPrint && complexItems) {
                writeNewLine();
                indentLevel++;
            }
            
            for (int i = 0; i < list.Count; i++) {
                if (i > 0) {
                    sb.Append(',');
                    if (prettyPrint) {
                        if (complexItems) {
                            writeNewLine();
                        } else {
                            sb.Append(' ');
                        }
                    } else {
                        sb.Append(' ');
                    }
                }
                
                if (prettyPrint && complexItems) {
                    writeIndent();
                }
                
                writeValue(list[i]);
            }
            
            if (prettyPrint && complexItems) {
                indentLevel--;
                writeNewLine();
                writeIndent();
            }
        }

        sb.Append(']');
    }

    private void writeArray<T>(string prefix, T[] data) {
        sb.Append('[').Append(prefix).Append("; ");
        
        for (int i = 0; i < data.Length; i++) {
            if (i > 0) sb.Append(", ");
            sb.Append(data[i]?.ToString() ?? "0");
        }

        sb.Append(']');
    }
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

        return c switch {
            '{' => parseCompound(name),
            '[' => parseListOrArray(name),
            '"' => new NBTString(name, parseQuotedString()),
            '-' or >= '0' and <= '9' => parseNumber(name),
            _ when isEndToken() => consumeEnd(),
            _ when isHexStruct() => parseHexStruct(name),
            _ when char.IsLetter(c) || c == '_' => parseUnquotedValue(name),
            _ => throw new FormatException($"Unexpected character at position {pos}: {c}")
        };
    }

    private bool isHexStruct() {
        return pos + 1 < input.Length && input[pos] == '0' && input[pos + 1] == 'x';
    }

    private NBTStruct parseHexStruct(string? name) {
        // consume "0x"
        pos += 2;

        var bytes = new List<byte>();
        while (pos + 1 < input.Length && isHexDigit(input[pos]) && isHexDigit(input[pos + 1])) {
            string hexByte = input.Substring(pos, 2);
            bytes.Add(Convert.ToByte(hexByte, 16));
            pos += 2;
        }

        return new NBTStruct(name, bytes.ToArray());
    }

    private static bool isHexDigit(char c) {
        return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
    }

    private bool isEndToken() {
        return input.Length - pos >= 3 && input.AsSpan(pos, 3) is "END";
    }

    private NBTEnd consumeEnd() {
        pos += 3;
        return new NBTEnd();
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
        return peek() == '"' ? parseQuotedString() : parseUnquotedKey();
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

    private NBTTag parseUnquotedValue(string? name) {
        string token = parseUnquotedKey();
        
        // Try to parse as number with suffix
        if (tryParseTypedNumber(token, name, out var numberTag)) {
            return numberTag;
        }
        
        // It's an unquoted string
        return new NBTString(name, token);
    }

    private bool tryParseTypedNumber(string token, string? name, out NBTTag result) {
        result = null!;
        
        // Check suffixes from longest to shortest
        if (token.EndsWith("ub") && isValidInteger(token.AsSpan(0, token.Length - 2))) {
            result = new NBTByte(name, byte.Parse(token[..^2]));
            return true;
        }
        if (token.EndsWith("us") && isValidInteger(token.AsSpan(0, token.Length - 2))) {
            result = new NBTUShort(name, ushort.Parse(token[..^2]));
            return true;
        }
        if (token.EndsWith("uL") && isValidInteger(token.AsSpan(0, token.Length - 2))) {
            result = new NBTULong(name, ulong.Parse(token[..^2]));
            return true;
        }
        if (token.EndsWith('b') && isValidInteger(token.AsSpan(0, token.Length - 1))) {
            result = new NBTByte(name, byte.Parse(token[..^1]));
            return true;
        }
        if (token.EndsWith('s') && isValidInteger(token.AsSpan(0, token.Length - 1))) {
            result = new NBTShort(name, short.Parse(token[..^1]));
            return true;
        }
        if (token.EndsWith('u') && isValidInteger(token.AsSpan(0, token.Length - 1))) {
            result = new NBTUInt(name, uint.Parse(token[..^1]));
            return true;
        }
        if (token.EndsWith('L') && isValidInteger(token.AsSpan(0, token.Length - 1))) {
            result = new NBTLong(name, long.Parse(token[..^1]));
            return true;
        }
        if (token.EndsWith('f') && isValidFloat(token.AsSpan(0, token.Length - 1))) {
            result = new NBTFloat(name, float.Parse(token[..^1], CultureInfo.InvariantCulture));
            return true;
        }
        if (token.EndsWith('d') && isValidFloat(token.AsSpan(0, token.Length - 1))) {
            result = new NBTDouble(name, double.Parse(token[..^1], CultureInfo.InvariantCulture));
            return true;
        }
        
        return false;
    }

    private static bool isValidInteger(ReadOnlySpan<char> s) {
        if (s.Length == 0) return false;
        int start = s[0] == '-' ? 1 : 0;
        if (start >= s.Length) return false;
        
        for (int i = start; i < s.Length; i++) {
            if (!char.IsDigit(s[i])) return false;
        }
        return true;
    }

    private static bool isValidFloat(ReadOnlySpan<char> s) {
        if (s.Length == 0) return false;
        int start = s[0] == '-' ? 1 : 0;
        if (start >= s.Length) return false;
        
        bool hasDot = false;
        for (int i = start; i < s.Length; i++) {
            if (s[i] == '.') {
                if (hasDot) return false;
                hasDot = true;
            } else if (!char.IsDigit(s[i])) {
                return false;
            }
        }
        return true;
    }

    private NBTTag parseListOrArray(string? name) {
        next(); // consume '['
        skipWhitespace();

        // Try array prefix
        int savedPos = pos;
        string? prefix = tryParseArrayPrefix();

        if (prefix != null) {
            return parseArray(name, prefix);
        }

        // Try empty list with type
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
        
        // Collect letters for prefix
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
        
        // Collect type name
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

    private static bool isValidArrayPrefix(string prefix) {
        return prefix is "B" or "S" or "US" or "I" or "UI" or "L" or "UL";
    }

    private static NBTType? parseTypeName(string name) {
        foreach (NBTType type in Enum.GetValues<NBTType>()) {
            if (NBTTag.getTypeName(type) == name) {
                return type;
            }
        }
        return null;
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
            return new NBTList<NBTEnd>(NBTType.TAG_End, name);
        }

        // Create typed list based on first element
        NBTType listType = items[0].id;
        var list = NBTTag.createListTag(listType, name);

        // Add items via reflection (unavoidable due to generic constraints)
        var listField = list.GetType().GetField("list");
        if (listField?.GetValue(list) is System.Collections.IList ilist) {
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
            if (c == '"') {
                break;
            }
            if (c == '\\') {
                char escaped = next();
                switch (escaped) {
                    case '"':
                        sb.Append('"');
                        break;
                    case '\\':
                        sb.Append('\\');
                        break;
                    default:
                        throw new FormatException($"Invalid escape sequence at position {pos - 1}");
                }
            }
            else {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private NBTTag parseNumber(string? name) {
        int start = pos;
        bool isFloat = false;

        if (peek() == '-') {
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
        int suffixStart = pos;
        while (pos < input.Length && char.IsLetter(input[pos])) {
            pos++;
        }

        string suffix = pos > suffixStart ? input.Substring(suffixStart, pos - suffixStart) : "";

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