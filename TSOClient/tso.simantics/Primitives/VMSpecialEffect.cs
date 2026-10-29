using FSO.SimAntics.Engine;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.Utils;
using System.IO;

namespace FSO.SimAntics.Primitives
{
    public class VMSpecialEffect : VMPrimitiveHandler
    {
        /// <summary>
        /// TS1's Auto-Centering option: when off, special events don't move the camera.
        /// </summary>
        public static bool AutoCenter = true;

        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            //string table 305 used by this primitive for screenshot caption
            var operand = (VMSpecialEffectOperand)args;
            if (context.VM.TS1 && VM.UseWorld)
            {
                if (((byte)operand.Flags & (1<<3)) > 0 && AutoCenter) context.VM.Context.World.CenterTo(context.StackObject.WorldUI);
                //snapshots and picture in picture are up to the game's UI
                context.VM.SignalGenericVMEvt(VMEventType.TS1SpecialEffect, new VMSpecialEffectEvent(context.StackObject, operand));
            }
            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    /// <summary>
    /// A special effect for the game's UI: a snapshot (flag 4, captioned from the object's string table 305) or a
    /// picture in picture window (flag 1) of the stack object, for Timeout seconds, at a size (1 small, 2 large) and zoom
    /// (1 far to 3 near). The flags' meanings are from how the game's objects use them.
    /// </summary>
    public class VMSpecialEffectEvent
    {
        public VMEntity Object;
        public VMSpecialEffectOperand Operand;
        public bool PIP => ((byte)Operand.Flags & (1 << 1)) > 0;
        public bool Snapshot => ((byte)Operand.Flags & (1 << 4)) > 0;

        public VMSpecialEffectEvent(VMEntity obj, VMSpecialEffectOperand operand)
        {
            Object = obj;
            Operand = operand;
        }

        /// <summary>
        /// The snapshot's caption, or "" if the object has none.
        /// </summary>
        public string Caption
        {
            get
            {
                var table = Object?.Object.Resource.Get<STR>(305);
                return table?.GetString(System.Math.Max(0, (int)Operand.StrIndex)) ?? "";
            }
        }
    }

    public class VMSpecialEffectOperand : VMPrimitiveOperand
    {
        public ushort Timeout;
        public sbyte Size;
        public sbyte Zoom;
        public sbyte Flags;
        public sbyte StrIndex;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                Timeout = io.ReadUInt16();
                Size = (sbyte)io.ReadByte();
                Zoom = (sbyte)io.ReadByte();
                Flags = (sbyte)io.ReadByte();
                StrIndex = (sbyte)io.ReadByte();
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(Timeout);
                io.Write(Size);
                io.Write(Zoom);
                io.Write(Flags);
                io.Write(StrIndex);
            }
        }
        #endregion
    }
}
