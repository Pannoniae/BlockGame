using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BlockGameTesting;

/** Test allocating class objects on the native heap - absolutely cursed but let's see if it works */
public unsafe class NativeHeapTest {

    // test class with some fields and a reference to another instance
    public class CursedObject {
        private static readonly IntPtr s_methodTable;

        static CursedObject() {
            // cache method table once - get it from a temp instance
            var temp = new CursedObject();
            // for reference types, the variable contains a pointer to the object header
            // object layout: [sync block][method table][fields...]
            // the object reference points to the method table location
            IntPtr objPtr = Unsafe.As<CursedObject, IntPtr>(ref temp);
            s_methodTable = *(IntPtr*)objPtr;  // read method table
        }

        public int value;
        public CursedObject? next;  // ref to another native object

        /** Allocate on native heap - returns normal reference! */
        public static CursedObject AllocNative() {
            // calc size: sync block + method table + fields
            int fieldSize = sizeof(int) + IntPtr.Size;  // int + reference
            int totalSize = IntPtr.Size + IntPtr.Size + fieldSize;

            void* basePtr = NativeMemory.Alloc((nuint)totalSize);

            // object layout: [sync block][method table][fields...]
            // object reference points to method table location
            void* objPtr = (byte*)basePtr + IntPtr.Size;

            // write object header
            *((IntPtr*)basePtr) = IntPtr.Zero;  // sync block at -IntPtr.Size from objPtr
            *((IntPtr*)objPtr) = s_methodTable;  // method table at objPtr

            // clear the fields
            Unsafe.InitBlock((byte*)objPtr + IntPtr.Size, 0, (uint)fieldSize);

            // reinterpret ptr as reference
            IntPtr ptrValue = (IntPtr)objPtr;
            return Unsafe.As<IntPtr, CursedObject>(ref ptrValue);
        }

        /** Free native object */
        public static void FreeNative(CursedObject obj) {
            IntPtr objPtr = Unsafe.As<CursedObject, IntPtr>(ref obj);
            void* basePtr = (byte*)objPtr - IntPtr.Size;  // back to sync block
            NativeMemory.Free(basePtr);
        }

        public int GetValue() => value;
        public void SetValue(int v) => value = v;
    }

    [Test]
    public void TestNativeAllocation() {
        CursedObject? obj1 = null;
        CursedObject? obj2 = null;

        try {
            // allocate two objects on native heap - LOOKS COMPLETELY NORMAL!
            obj1 = CursedObject.AllocNative();
            obj2 = CursedObject.AllocNative();

            // basic field access
            obj1.value = 42;
            obj2.value = 69;

            Assert.That(obj1.value, Is.EqualTo(42));
            Assert.That(obj2.value, Is.EqualTo(69));

            // method calls
            obj1.SetValue(100);
            Assert.That(obj1.GetValue(), Is.EqualTo(100));

            // ref to another native object - completely normal assignment:tm:
            obj1.next = obj2;
            Assert.That(obj1.next, Is.Not.Null);
            Assert.That(obj1.next!.value, Is.EqualTo(69));

            // chain access
            obj1.next.value = 420;
            Assert.That(obj2.value, Is.EqualTo(420));

            // trigger GC - should not crash since these are invisible to GC
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // verify objects still work after GC
            Assert.That(obj1.value, Is.EqualTo(100));
            Assert.That(obj2.value, Is.EqualTo(420));
            Assert.That(obj1.next!.value, Is.EqualTo(420));
        }
        finally {
            // cleanup
            if (obj1 != null) CursedObject.FreeNative(obj1);
            if (obj2 != null) CursedObject.FreeNative(obj2);
        }
    }

    [Test]
    public void TestNativeChain() {
        const int chainLen = 10;
        CursedObject?[] chain = new CursedObject?[chainLen];

        try {
            // allocate chain of objects
            for (int i = 0; i < chainLen; i++) {
                chain[i] = CursedObject.AllocNative();
                chain[i]!.value = i * 100;

                if (i > 0) {
                    chain[i - 1]!.next = chain[i];
                }
            }

            // walk the chain
            CursedObject? current = chain[0];
            int count = 0;
            while (current != null) {
                Assert.That(current.value, Is.EqualTo(count * 100));
                current = current.next;
                count++;
            }

            Assert.That(count, Is.EqualTo(chainLen));

            // trigger aggressive GC
            for (int i = 0; i < 3; i++) {
                GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                GC.WaitForPendingFinalizers();
            }

            // verify chain still intact
            current = chain[0];
            count = 0;
            while (current != null) {
                Assert.That(current.value, Is.EqualTo(count * 100));
                current = current.next;
                count++;
            }

            Assert.That(count, Is.EqualTo(chainLen));

            TestContext.Out.WriteLine($"Chain of {chainLen} native objects survived aggressive GC lol");
        }
        finally {
            // cleanup all
            for (int i = 0; i < chainLen; i++) {
                if (chain[i] != null) {
                    CursedObject.FreeNative(chain[i]!);
                }
            }
        }
    }

    [Test]
    public void TestCompletelyNormalSyntax() {
        CursedObject? obj1 = null;
        CursedObject? obj2 = null;
        CursedObject? obj3 = null;

        try {
            // if you didn't know about AllocNative, this looks like normal C# code!
            obj1 = CursedObject.AllocNative();
            obj2 = CursedObject.AllocNative();
            obj3 = CursedObject.AllocNative();

            obj1.value = 100;
            obj2.value = 200;
            obj3.value = 300;

            obj1.next = obj2;
            obj2.next = obj3;

            // traverse
            Assert.That(obj1.next.value, Is.EqualTo(200));
            Assert.That(obj1.next.next!.value, Is.EqualTo(300));

            // modify through chain
            obj1.next.value = 999;
            Assert.That(obj2.value, Is.EqualTo(999));

            // method calls
            obj3.SetValue(42);
            Assert.That(obj3.GetValue(), Is.EqualTo(42));

            // AGGRESSIVE GC TORTURE TEST
            TestContext.Out.WriteLine("running GC...");
            for (int i = 0; i < 10; i++) {
                GC.Collect(2, GCCollectionMode.Aggressive, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            }

            // still alive!
            Assert.That(obj1.value, Is.EqualTo(100));
            Assert.That(obj2.value, Is.EqualTo(999));
            Assert.That(obj3.value, Is.EqualTo(42));
            Assert.That(obj1.next, Is.Not.Null);
            Assert.That(obj1.next.next, Is.Not.Null);
        }
        finally {
            if (obj1 != null) CursedObject.FreeNative(obj1);
            if (obj2 != null) CursedObject.FreeNative(obj2);
            if (obj3 != null) CursedObject.FreeNative(obj3);
        }
    }
}