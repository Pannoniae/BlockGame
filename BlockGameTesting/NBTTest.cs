using BlockGame.util.xNBT;

namespace BlockGameTesting;

using System;
using System.IO;
using NUnit;
using NUnit.Framework;

public class NBTTest {
    [Test]
    public void TestPrimitives() {
        // Test all primitive types
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

    [Test]
    public void TestStringEscaping() {
        AssertRoundtrip(new NBTString("", "Quote: \"test\""), "\"Quote: \\\"test\\\"\"");
        AssertRoundtrip(new NBTString("", "Backslash: \\test\\"), "\"Backslash: \\\\test\\\\\"");
        AssertRoundtrip(new NBTString("", "Both: \"\\\""), "\"Both: \\\"\\\\\\\"\"");
    }

    [Test]
    public void TestArrays() {
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

    [Test]
    public void TestCompound() {
        var compound = new NBTCompound("test");
        compound.addByte("byte", 1);
        compound.addInt("int", 42);
        compound.addString("str", "test");
        
        var parsed = SNBT.parse(SNBT.toString(compound)) as NBTCompound;
        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed.getByte("byte"), Is.EqualTo(1));
        Assert.That(parsed.getInt("int"), Is.EqualTo(42));
        Assert.That(parsed.getString("str"), Is.EqualTo("test"));
    }

    [Test]
    public void TestNestedCompound() {
        var root = new NBTCompound("root");
        var child = new NBTCompound("child");
        child.addInt("value", 123);
        root.addCompoundTag("child", child);
        
        var grandchild = new NBTCompound("grandchild");
        grandchild.addString("deep", "nested");
        child.addCompoundTag("grandchild", grandchild);
        
        var parsed = SNBT.parse(SNBT.toString(root)) as NBTCompound;
        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed.getCompoundTag("child").getInt("value"), Is.EqualTo(123));
        Assert.That(parsed.getCompoundTag("child").getCompoundTag("grandchild").getString("deep"), Is.EqualTo("nested"));
    }

    [Test]
    public void TestLists() {
        // List of ints
        var intList = new NBTList<NBTInt>(NBTType.TAG_Int, "test");
        intList.add(new NBTInt(null, 1));
        intList.add(new NBTInt(null, 2));
        intList.add(new NBTInt(null, 3));
        AssertRoundtrip(intList, "[1, 2, 3]");
        
        // List of strings
        var strList = new NBTList<NBTString>(NBTType.TAG_String, "test");
        strList.add(new NBTString(null, "hello"));
        strList.add(new NBTString(null, "world"));
        AssertRoundtrip(strList, "[\"hello\", \"world\"]");
        
        // List of compounds
        var compList = new NBTList<NBTCompound>(NBTType.TAG_Compound, "test");
        var comp1 = new NBTCompound(null);
        comp1.addInt("x", 1);
        var comp2 = new NBTCompound(null);
        comp2.addInt("x", 2);
        compList.add(comp1);
        compList.add(comp2);
        
        var parsed = SNBT.parse(SNBT.toString(compList));
        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed, Is.TypeOf<NBTList<NBTTag>>());
    }

    [Test]
    public void TestEmptyLists() {
        // Empty list of each type
        AssertRoundtrip(new NBTList<NBTByte>(NBTType.TAG_Byte, ""), "[TAG_Byte;]");
        AssertRoundtrip(new NBTList<NBTShort>(NBTType.TAG_Short, ""), "[TAG_Short;]");
        AssertRoundtrip(new NBTList<NBTInt>(NBTType.TAG_Int, ""), "[TAG_Int;]");
        AssertRoundtrip(new NBTList<NBTString>(NBTType.TAG_String, ""), "[TAG_String;]");
        AssertRoundtrip(new NBTList<NBTCompound>(NBTType.TAG_Compound, ""), "[TAG_Compound;]");
        AssertRoundtrip(new NBTList<NBTList<NBTTag>>(NBTType.TAG_List, ""), "[TAG_List;]");
    }

    [Test]
    public void TestComplexStructure() {
        var root = new NBTCompound("root");
        
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
        
        foreach (var (snbt, expected) in testCases) {
            var parsed = SNBT.parse(snbt) as NBTCompound;
            Assert.That(parsed, Is.Not.Null);
            Assert.That(expected, Is.EqualTo(parsed.getInt("key")));
        }
    }

    [Test]
    public void TestErrorHandling() {
        // Invalid syntax should throw
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

    [Test]
    public void TestBinaryCompatibility() {
        // Create a complex NBT structure
        var root = new NBTCompound("root");
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