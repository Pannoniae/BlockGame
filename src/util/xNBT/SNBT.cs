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
    public static string toString(NBTTag tag) {
        var writer = new SNBTWriter();
        return writer.write(tag);
    }

    public static NBTTag parse(string snbt) {
        var parser = new SNBTParser(snbt);
        return parser.parse();
    }
}

internal class SNBTWriter {
    private readonly StringBuilder sb = new();

    public string write(NBTTag tag) {
        writeValue(tag);
        return sb.ToString();
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
        sb.Append('"');
        foreach (char c in s) {
            if (c == '"') sb.Append("\\\"");
            else if (c == '\\') sb.Append(@"\\");
            else sb.Append(c);
        }
        sb.Append('"');
    }

    private void writeCompound(NBTCompound compound) {
        sb.Append('{');
        bool first = true;
        foreach (var kvp in compound.dict) {
            if (!first) sb.Append(", ");
            first = false;
            writeString(kvp.Key);
            sb.Append(": ");
            writeValue(kvp.Value);
        }
        sb.Append('}');
    }

    private void writeList(List<NBTTag> list, NBTType listType) {
        sb.Append('[');
        if (list.Count == 0) {
            sb.Append(NBTTag.getTypeName(listType)).Append(';');
        } else {
            for (int i = 0; i < list.Count; i++) {
                if (i > 0) sb.Append(", ");
                writeValue(list[i]);
            }
        }
        sb.Append(']');
    }

    private void writeArray<T>(string prefix, NBTTag t, T[] data) {
        sb.Append('[').Append(prefix).Append("; ");
        // don't actually do this lol
        //if (data.Length == 0) {
        //    sb.Append(NBTTag.getTypeName(t.id)).Append(';');
        //}

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

        if (c == '{') {
            return parseCompound(name);
        } else if (c == '[') {
            return parseListOrArray(name);
        } else if (c == '"') {
            return new NBTString(name, parseString());
        } else if (c == '-' || char.IsDigit(c)) {
            return parseNumber(name);
        } else if (input.Length - pos >= 3 && input.Substring(pos, 3) == "END") {
            pos += 3;
            return new NBTEnd();
        } else {
            throw new FormatException($"Unexpected character at position {pos}: {c}");
        }
    }

    private NBTCompound parseCompound(string? name) {
        next(); // consume '{'
        var compound = new NBTCompound(name);
        skipWhitespace();

        while (peek() != '}') {
            string key = parseString();
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
            } else if (peek() != '}') {
                throw new FormatException($"Expected ',' or '}}' at position {pos}");
            }
        }
        next(); // consume '}'
        return compound;
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
            return createEmptyList(emptyListType.Value, name);
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
        foreach (NBTType type in Enum.GetValues(typeof(NBTType))) {
            if (NBTTag.getTypeName(type) == name) {
                return type;
            }
        }
        return null;
    }

    private NBTTag createEmptyList(NBTType listType, string? name) {
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
            } else if (peek() != ']') {
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
            } else if (peek() != ']') {
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
        var list = createEmptyList(listType, name);
        
        // Add items using reflection (ugly but necessary due to generic constraints)
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

    private string parseString() {
        if (next() != '"') {
            throw new FormatException($"Expected '\"' at position {pos - 1}");
        }
        
        var sb = new StringBuilder();
        while (true) {
            char c = next();
            if (c == '\0') {
                throw new FormatException("Unterminated string");
            } else if (c == '"') {
                break;
            } else if (c == '\\') {
                char escaped = next();
                if (escaped == '"') sb.Append('"');
                else if (escaped == '\\') sb.Append('\\');
                else throw new FormatException($"Invalid escape sequence at position {pos - 1}");
            } else {
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
            } else if (c == '.' && !isFloat) {
                isFloat = true;
                pos++;
            } else {
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