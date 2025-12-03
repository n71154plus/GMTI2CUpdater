using System;
using MoonSharp.Interpreter;

namespace GMTI2CUpdater.I2CAdapter.Unlock
{
    [MoonSharpUserData]
    internal class LuaTconContext
    {
        private readonly Script script;
        private readonly I2CAdapterBase adapter;

        public LuaTconContext(Script script, I2CAdapterBase adapter)
        {
            this.script = script ?? throw new ArgumentNullException(nameof(script));
            this.adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

        public Table ReadDpcd(uint address, uint count)
        {
            return BytesToTable(adapter.ReadDpcd(address, count));
        }

        public void WriteDpcd(uint address, DynValue data)
        {
            adapter.WriteDpcd(address, ToByteArray(data));
        }

        public Table ReadI2CByteIndex(byte address, byte index, int length)
        {
            return BytesToTable(adapter.ReadI2CByteIndex(address, index, length));
        }

        public void WriteI2CByteIndex(byte address, byte index, DynValue data)
        {
            adapter.WriteI2CByteIndex(address, index, ToByteArray(data));
        }

        public Table ReadI2CUInt16Index(byte address, ushort index, int length)
        {
            return BytesToTable(adapter.ReadI2CUInt16Index(address, index, length));
        }

        public void WriteI2CUInt16Index(byte address, ushort index, DynValue data)
        {
            adapter.WriteI2CUInt16Index(address, index, ToByteArray(data));
        }

        public double ReadI2CWithoutIndex(byte address)
        {
            return adapter.ReadI2CWithoutIndex(address);
        }

        public void WriteI2CWithoutIndex(byte address, byte data)
        {
            adapter.WriteI2CWithoutIndex(address, data);
        }

        private byte[] ToByteArray(DynValue value)
        {
            return value.Type switch
            {
                DataType.Table => TableToBytes(value.Table),
                DataType.Number => new[] { ToByte(value) },
                _ => throw new ScriptRuntimeException($"Expected table or number but received {value.Type}.")
            };
        }

        private byte[] TableToBytes(Table table)
        {
            int length = (int)table.Length;
            var result = new byte[length];
            for (int i = 0; i < length; i++)
            {
                var element = table.Get(i + 1);
                if (element.IsNil())
                {
                    throw new ScriptRuntimeException($"Table element at position {i + 1} is nil.");
                }
                result[i] = ToByte(element);
            }
            return result;
        }

        private Table BytesToTable(byte[] data)
        {
            var table = new Table(script);
            for (int i = 0; i < data.Length; i++)
            {
                table[i + 1] = DynValue.NewNumber(data[i]);
            }
            return table;
        }

        private static byte ToByte(DynValue value)
        {
            if (value.Type != DataType.Number)
            {
                throw new ScriptRuntimeException($"Expected number but received {value.Type}.");
            }

            var number = value.Number;
            if (number < byte.MinValue || number > byte.MaxValue)
            {
                throw new ScriptRuntimeException($"Value {number} is outside the byte range.");
            }

            return (byte)number;
        }
    }
}
