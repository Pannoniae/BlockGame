using BlockGame.util.xNBT;

namespace BlockGameTesting;

using System;
using System.IO;
using NUnit.Framework;

public class NBTTest {
    [Test]
    public void TestPrimitives() {
        // Test all primitive types
        using (Assert.EnterMultipleScope()) {
            AssertRoundtrip(new NBTByte("", 127), "127b");
            AssertRoundtrip(new NBTByte("", 255), "255b"); // unsigned byte
            AssertRoundtrip(new NBTShort("", -32768), "-32768s");
            AssertRoundtrip(new NBTUShort("", 65535), "65535us");
            AssertRoundtrip(new NBTInt("", -2147483648), "-2147483648");
            AssertRoundtrip(new NBTUInt("", 4294967295), "4294967295u");
            AssertRoundtrip(new NBTLong("", -9223372036854775808), "-9223372036854775808L");
            AssertRoundtrip(new NBTULong("", 18446744073709551615), "18446744073709551615uL");
            AssertRoundtrip(new NBTFloat("", 3.14159f), "3.14159f");
            AssertRoundtrip(new NBTDouble("", 2.718281828), "2.718281828d");
            AssertRoundtrip(new NBTString("", "Hello, World!"), "\"Hello, World!\"");
        }
    }

    [Test]
    public void TestStringEscaping() {
        using (Assert.EnterMultipleScope()) {
            AssertRoundtrip(new NBTString("", "Quote: \"test\""), "\"Quote: \\\"test\\\"\"");
            AssertRoundtrip(new NBTString("", "Backslash: \\test\\"), "\"Backslash: \\\\test\\\\\"");
            AssertRoundtrip(new NBTString("", "Both: \"\\\""), "\"Both: \\\"\\\\\\\"\"");
        }
    }

    [Test]
    public void TestArrays() {
        using (Assert.EnterMultipleScope()) {
            AssertRoundtrip(new NBTByteArray("", [1, 2, 3]), "[B; 1, 2, 3]");
            AssertRoundtrip(new NBTShortArray("", [-1, 0, 1]), "[S; -1, 0, 1]");
            AssertRoundtrip(new NBTUShortArray("", [0, 32768, 65535]), "[US; 0, 32768, 65535]");
            AssertRoundtrip(new NBTIntArray("", [-1000, 0, 1000]), "[I; -1000, 0, 1000]");
            AssertRoundtrip(new NBTUIntArray("", [0, 2147483648]), "[UI; 0, 2147483648]");
            AssertRoundtrip(new NBTLongArray("", [-1L, 0L, 1L]), "[L; -1, 0, 1]");
            AssertRoundtrip(new NBTULongArray("", [0, 9223372036854775808]), "[UL; 0, 9223372036854775808]");

            // Empty arrays
            AssertRoundtrip(new NBTByteArray("", []), "[B; ]");
            AssertRoundtrip(new NBTIntArray("", []), "[I; ]");
        }
    }

    [Test]
    public void TestCompound() {
        var compound = new NBTCompound("");
        compound.addByte("byte", 1);
        compound.addInt("int", 42);
        compound.addString("str", "test");

        var parsed = SNBT.parse(SNBT.toString(compound)) as NBTCompound;

        using (Assert.EnterMultipleScope()) {
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed.getByte("byte"), Is.EqualTo(1));
            Assert.That(parsed.getInt("int"), Is.EqualTo(42));
            Assert.That(parsed.getString("str"), Is.EqualTo("test"));
        }
    }

    [Test]
    public void TestNestedCompound() {
        var root = new NBTCompound("");
        var child = new NBTCompound("child");
        child.addInt("value", 123);
        root.addCompoundTag("child", child);

        var grandchild = new NBTCompound("grandchild");
        grandchild.addString("deep", "nested");
        child.addCompoundTag("grandchild", grandchild);

        var parsed = SNBT.parse(SNBT.toString(root)) as NBTCompound;

        using (Assert.EnterMultipleScope()) {
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed.getCompoundTag("child").getInt("value"), Is.EqualTo(123));
            Assert.That(parsed.getCompoundTag("child").getCompoundTag("grandchild").getString("deep"),
                Is.EqualTo("nested"));
        }
    }

    [Test]
    public void TestLists() {
        // List of ints
        var intList = new NBTList<NBTInt>(NBTType.TAG_Int, "");
        intList.add(new NBTInt(null, 1));
        intList.add(new NBTInt(null, 2));
        intList.add(new NBTInt(null, 3));
        AssertRoundtrip(intList, "[1, 2, 3]");

        // List of strings
        var strList = new NBTList<NBTString>(NBTType.TAG_String, "");
        strList.add(new NBTString(null, "hello"));
        strList.add(new NBTString(null, "world"));
        AssertRoundtrip(strList, "[hello, world]");

        // List of compounds
        var compList = new NBTList<NBTCompound>(NBTType.TAG_Compound, "");
        var comp1 = new NBTCompound(null);
        comp1.addInt("x", 1);
        var comp2 = new NBTCompound(null);
        comp2.addInt("x", 2);
        compList.add(comp1);
        compList.add(comp2);

        using (Assert.EnterMultipleScope()) {
            var parsed = SNBT.parse(SNBT.toString(compList));
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed, Is.TypeOf<NBTList<NBTCompound>>());

            // test binary NBT too
            var parsedBinary = NBT.read(NBT.write(compList));
            Assert.That(parsedBinary, Is.Not.Null);
            Assert.That(parsedBinary, Is.TypeOf<NBTList<NBTCompound>>());
        }
    }

    [Test]
    public void TestEmptyLists() {
        // Empty list of each type
        using (Assert.EnterMultipleScope()) {
            AssertRoundtrip(new NBTList<NBTByte>(NBTType.TAG_Byte, ""), "[TAG_Byte;]");
            AssertRoundtrip(new NBTList<NBTShort>(NBTType.TAG_Short, ""), "[TAG_Short;]");
            AssertRoundtrip(new NBTList<NBTInt>(NBTType.TAG_Int, ""), "[TAG_Int;]");
            AssertRoundtrip(new NBTList<NBTString>(NBTType.TAG_String, ""), "[TAG_String;]");
            AssertRoundtrip(new NBTList<NBTCompound>(NBTType.TAG_Compound, ""), "[TAG_Compound;]");
            AssertRoundtrip(new NBTList<NBTList<NBTTag>>(NBTType.TAG_List, ""), "[TAG_List;]");
        }
    }

    [Test]
    public void TestComplexStructure() {
        var root = new NBTCompound("");

        // Add various primitives
        root.addByte("byte", 127);
        root.addShort("short", 32767);
        root.addInt("int", 2147483647);
        root.addLong("long", 9223372036854775807);
        root.addFloat("float", 1.23f);
        root.addDouble("double", 4.56);
        root.addString("string", "Hello \"World\"!");

        // Add arrays
        root.addByteArray("bytes", [1, 2, 3]);
        root.addIntArray("ints", [10, 20, 30]);

        // Add nested compound
        var nested = new NBTCompound("nested");
        nested.addString("key", "value");
        root.addCompoundTag("nested", nested);

        // Add list
        var list = new NBTList<NBTInt>(NBTType.TAG_Int, "list");
        list.add(new NBTInt(null, 100));
        list.add(new NBTInt(null, 200));
        root.addListTag("list", list);

        // Test roundtrip
        string snbt = SNBT.toString(root);
        var parsed = SNBT.parse(snbt) as NBTCompound;

        using (Assert.EnterMultipleScope()) {
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed.getByte("byte"), Is.EqualTo(127));
            Assert.That(parsed.getShort("short"), Is.EqualTo(32767));
            Assert.That(parsed.getInt("int"), Is.EqualTo(2147483647));
            Assert.That(parsed.getLong("long"), Is.EqualTo(9223372036854775807));
            Assert.That(parsed.getFloat("float"), Is.EqualTo(1.23f).Within(0.00001f));
            Assert.That(parsed.getDouble("double"), Is.EqualTo(4.56).Within(0.0000000001));
            Assert.That(parsed.getString("string"), Is.EqualTo("Hello \"World\"!"));
            Assert.That(parsed.getByteArray("bytes"), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(parsed.getIntArray("ints"), Is.EqualTo([10, 20, 30]));
            Assert.That(parsed.getCompoundTag("nested").getString("key"), Is.EqualTo("value"));
            Assert.That(parsed.getListTag<NBTInt>("list").count(), Is.EqualTo(2));
        }
    }

    [Test]
    public void TestRootNonCompound() {
        // Test that any tag type can be at root
        AssertRoundtrip(new NBTInt(null, 42), "42");
        AssertRoundtrip(new NBTString(null, "root string"), "\"root string\"");

        var list = new NBTList<NBTInt>(NBTType.TAG_Int, null);
        list.add(new NBTInt(null, 1));
        list.add(new NBTInt(null, 2));
        AssertRoundtrip(list, "[1, 2]");

        AssertRoundtrip(new NBTByteArray(null, [1, 2, 3]), "[B; 1, 2, 3]");
    }

    [Test]
    public void TestWhitespaceHandling() {
        // Parser should handle whitespace correctly
        var testCases = new[] {
            ("{ \"key\" : 123 }", 123),
            ("{\"key\":123}", 123),
            ("  {  \"key\"  :  123  }  ", 123),
            ("{\n  \"key\": 123\n}", 123),
            ("{\t\"key\":\t123\t}", 123)
        };
        using (Assert.EnterMultipleScope()) {
            foreach (var (snbt, expected) in testCases) {
                var parsed = SNBT.parse(snbt) as NBTCompound;
                Assert.That(parsed, Is.Not.Null);
                Assert.That(expected, Is.EqualTo(parsed.getInt("key")));
            }
        }
    }

    [Test]
    public void TestErrorHandling() {
        // Invalid syntax should throw
        using (Assert.EnterMultipleScope()) {
            Assert.Throws<FormatException>(() => SNBT.parse("{"));
            Assert.Throws<FormatException>(() => SNBT.parse("{ \"key\" }"));
            Assert.Throws<FormatException>(() => SNBT.parse("{ \"key\": }"));
            Assert.Throws<FormatException>(() => SNBT.parse("[1, 2, 3"));
            Assert.Throws<FormatException>(() => SNBT.parse("\"unterminated string"));
            Assert.Throws<FormatException>(() => SNBT.parse("123xyz")); // invalid suffix
            Assert.Throws<FormatException>(() => SNBT.parse("[X; 1, 2, 3]")); // invalid array type

            // Mixed types in list should throw
            Assert.Throws<FormatException>(() => SNBT.parse("[1, \"string\"]"));
            Assert.Throws<FormatException>(() => SNBT.parse("[1, 2.5f]"));
        }
    }

    [Test]
    public void TestQuotedAndUnquotedStrings() {
        // Test unquoted strings (valid identifiers)
        using (Assert.EnterMultipleScope()) {
            AssertRoundtrip(new NBTString("", "hello"), "hello");
            AssertRoundtrip(new NBTString("", "world123"), "world123");
            AssertRoundtrip(new NBTString("", "valid_name"), "valid_name");
            AssertRoundtrip(new NBTString("", "with.dots"), "with.dots");
            AssertRoundtrip(new NBTString("", "with+plus"), "with+plus");
            AssertRoundtrip(new NBTString("", "with-minus"), "with-minus");
            AssertRoundtrip(new NBTString("", "_underscore"), "_underscore");
        }

        // Test strings that must be quoted (invalid as unquoted)
        using (Assert.EnterMultipleScope()) {
            AssertRoundtrip(new NBTString("", ""), "\"\""); // empty string
            AssertRoundtrip(new NBTString("", "123"), "\"123\""); // starts with number
            AssertRoundtrip(new NBTString("", "hello world"), "\"hello world\""); // contains space
            AssertRoundtrip(new NBTString("", "special@chars"), "\"special@chars\""); // special chars
            AssertRoundtrip(new NBTString("", "with:colon"), "\"with:colon\""); // colon
            AssertRoundtrip(new NBTString("", "with,comma"), "\"with,comma\""); // comma
            AssertRoundtrip(new NBTString("", "with{brace"), "\"with{brace\""); // brace
            AssertRoundtrip(new NBTString("", "with[bracket"), "\"with[bracket\""); // bracket
        }

        // Test reserved suffixes that must be quoted
        using (Assert.EnterMultipleScope()) {
            AssertRoundtrip(new NBTString("", "b"), "\"b\"");
            AssertRoundtrip(new NBTString("", "ub"), "\"ub\"");
            AssertRoundtrip(new NBTString("", "s"), "\"s\"");
            AssertRoundtrip(new NBTString("", "us"), "\"us\"");
            AssertRoundtrip(new NBTString("", "u"), "\"u\"");
            AssertRoundtrip(new NBTString("", "L"), "\"L\"");
            AssertRoundtrip(new NBTString("", "uL"), "\"uL\"");
            AssertRoundtrip(new NBTString("", "f"), "\"f\"");
            AssertRoundtrip(new NBTString("", "d"), "\"d\"");
            AssertRoundtrip(new NBTString("", "END"), "\"END\"");
        }

        // Test that parser can handle both quoted and unquoted in compounds
        var compound = new NBTCompound("");
        compound.addString("unquoted_key", "unquoted_value");
        compound.addString("quoted key", "quoted value with spaces");
        compound.addString("123", "number_key_quoted");

        var snbt = SNBT.toString(compound);
        var parsed = SNBT.parse(snbt) as NBTCompound;
        using (Assert.EnterMultipleScope()) {
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed.getString("unquoted_key"), Is.EqualTo("unquoted_value"));
            Assert.That(parsed.getString("quoted key"), Is.EqualTo("quoted value with spaces"));
            Assert.That(parsed.getString("123"), Is.EqualTo("number_key_quoted"));
        }

        // Test parsing manually constructed SNBT with mixed quoting
        var mixedParsed =
            SNBT.parse("{unquoted: hello, \"quoted\": \"world with spaces\", \"123\": number_start}") as NBTCompound;
        using (Assert.EnterMultipleScope()) {
            Assert.That(mixedParsed, Is.Not.Null);
            Assert.That(mixedParsed.getString("unquoted"), Is.EqualTo("hello"));
            Assert.That(mixedParsed.getString("quoted"), Is.EqualTo("world with spaces"));
            Assert.That(mixedParsed.getString("123"), Is.EqualTo("number_start"));
        }
    }

    [Test]
    public void TestEmptyNBTList() {
        // Test empty list roundtrip with type preservation
        var emptyByteList = new NBTList<NBTByte>(NBTType.TAG_Byte, "test");
        
        // Verify initial state
        Assert.That(emptyByteList.listType, Is.EqualTo(NBTType.TAG_Byte));
        Assert.That(emptyByteList.count(), Is.Zero);

        // Convert to SNBT - should include type information
        string snbt = SNBT.toString(emptyByteList);
        Assert.That(snbt, Is.EqualTo("[TAG_Byte;]"));

        // Parse it back
        var parsed = SNBT.parse(snbt);
        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed, Is.InstanceOf<INBTList>());
        
        var parsedList = parsed as INBTList;
        Assert.That(parsedList, Is.Not.Null);
        Assert.That(parsedList.listType, Is.EqualTo(NBTType.TAG_Byte));

        // Convert back to SNBT - should be identical
        string snbt2 = SNBT.toString(parsed);
        Assert.That(snbt2, Is.EqualTo(snbt));
        Assert.That(snbt2, Is.EqualTo("[TAG_Byte;]"));

        // Test with different empty list types
        using (Assert.EnterMultipleScope()) {
            var testCases = new (NBTTag, string)[] {
                (new NBTList<NBTShort>(NBTType.TAG_Short, ""), "[TAG_Short;]"),
                (new NBTList<NBTInt>(NBTType.TAG_Int, ""), "[TAG_Int;]"),
                (new NBTList<NBTString>(NBTType.TAG_String, ""), "[TAG_String;]"),
                (new NBTList<NBTCompound>(NBTType.TAG_Compound, ""), "[TAG_Compound;]"),
                (new NBTList<NBTFloat>(NBTType.TAG_Float, ""), "[TAG_Float;]"),
                (new NBTList<NBTDouble>(NBTType.TAG_Double, ""), "[TAG_Double;]")
            };

            foreach (var (list, expectedSnbt) in testCases) {
                Assert.That(((INBTList)list).count(), Is.Zero);
                
                string listSnbt = SNBT.toString(list);
                Assert.That(listSnbt, Is.EqualTo(expectedSnbt));
                
                var parsedListTest = SNBT.parse(listSnbt);
                Assert.That(parsedListTest, Is.Not.Null);
                Assert.That(((INBTList)parsedListTest).listType, Is.EqualTo(((INBTList)list).listType));
                
                string listSnbt2 = SNBT.toString(parsedListTest);
                Assert.That(listSnbt2, Is.EqualTo(expectedSnbt));
            }
        }
    }

    [Test]
    public void TestBinaryCompatibility() {
        // Create a complex NBT structure
        var root = new NBTCompound("");
        root.addString("name", "Test");
        root.addInt("version", 1);

        var pos = new NBTCompound("pos");
        pos.addDouble("x", 123.456);
        pos.addDouble("y", 78.9);
        pos.addDouble("z", -45.6);
        root.addCompoundTag("pos", pos);

        var items = new NBTList<NBTCompound>(NBTType.TAG_Compound, "items");
        for (int i = 0; i < 3; i++) {
            var item = new NBTCompound(null);
            item.addString("id", $"item_{i}");
            item.addInt("count", i + 1);
            items.add(item);
        }

        root.addListTag("items", items);

        // Convert to SNBT and back
        string snbt = SNBT.toString(root);
        var fromSnbt = SNBT.parse(snbt) as NBTCompound;

        // Write both to binary and compare
        using var originalStream = new MemoryStream();
        using var snbtStream = new MemoryStream();

        using (var writer = new BinaryWriter(originalStream)) {
            NBTTag.write(root, writer);
        }

        using (var writer = new BinaryWriter(snbtStream)) {
            NBTTag.write(fromSnbt!, writer);
        }

        // Binary representations should be identical
        var originalBytes = originalStream.ToArray();
        var snbtBytes = snbtStream.ToArray();

        Assert.That(originalBytes, Is.EqualTo(snbtBytes));
    }

    private void AssertRoundtrip(NBTTag tag, string expectedSnbt) {
        // Test toString
        string snbt = SNBT.toString(tag);
        Assert.That(snbt, Is.EqualTo(expectedSnbt));

        // Test parse
        var parsed = SNBT.parse(snbt);

        // Verify they produce the same binary output
        using var originalStream = new MemoryStream();
        using var parsedStream = new MemoryStream();

        using (var writer = new BinaryWriter(originalStream)) {
            NBTTag.write(tag, writer);
        }

        using (var writer = new BinaryWriter(parsedStream)) {
            NBTTag.write(parsed, writer);
        }

        var originalVal = originalStream.ToArray();
        var parsedVal = parsedStream.ToArray();

        Assert.That(originalVal, Is.EqualTo(parsedVal));

        // Double roundtrip
        string snbt2 = SNBT.toString(parsed);
        Assert.That(snbt2, Is.EqualTo(expectedSnbt));
    }
}