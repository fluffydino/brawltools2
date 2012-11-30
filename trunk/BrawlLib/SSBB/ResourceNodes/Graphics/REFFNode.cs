﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrawlLib.SSBBTypes;
using System.ComponentModel;
using BrawlLib.Imaging;
using System.Windows.Forms;
using BrawlLib.Wii.Graphics;

namespace BrawlLib.SSBB.ResourceNodes
{
    public unsafe class REFFNode : ARCEntryNode
    {
        internal REFF* Header { get { return (REFF*)WorkingUncompressed.Address; } }
        public override ResourceType ResourceType { get { return ResourceType.REFF; } }

        private int _version;

        private int _unk1, _unk2, _unk3, _dataLen, _dataOff;
        private int _TableLen;
        private short _TableEntries;
        private short _TableUnk1;

        [Category("REFF Data")]
        public int Version { get { return _version; } }
        [Category("REFF Data")]
        public int DataLength { get { return _dataLen; } }
        [Category("REFF Data")]
        public int DataOffset { get { return _dataOff; } }
        [Category("REFF Data")]
        public int Unknown1 { get { return _unk1; } set { _unk1 = value; SignalPropertyChange(); } }
        [Category("REFF Data")]
        public int Unknown2 { get { return _unk2; } set { _unk2 = value; SignalPropertyChange(); } }
        [Category("REFF Data")]
        public int Unknown3 { get { return _unk3; } set { _unk3 = value; SignalPropertyChange(); } }

        [Category("REFF Object Table")]
        public int Length { get { return _TableLen; } }
        [Category("REFF Object Table")]
        public short NumEntries { get { return _TableEntries; } }
        [Category("REFF Object Table")]
        public short Unk1 { get { return _TableUnk1; } set { _TableUnk1 = value; SignalPropertyChange(); } }

        protected override bool OnInitialize()
        {
            base.OnInitialize();

            REFF* header = Header;

            _version = header->_header._version;
            _name = header->IdString;
            _dataLen = header->_dataLength;
            _dataOff = header->_dataOffset;
            _unk1 = header->_linkPrev;
            _unk2 = header->_linkNext;
            _unk3 = header->_padding;

            REFTypeObjectTable* objTable = header->Table;
            _TableLen = (int)objTable->_length;
            _TableEntries = (short)objTable->_entries;
            _TableUnk1 = (short)objTable->_unk1;

            return header->Table->_entries > 0;
        }

        protected override void OnPopulate()
        {
            REFTypeObjectTable* table = Header->Table;
            REFTypeObjectEntry* Entry = table->First;
            for (int i = 0; i < table->_entries; i++, Entry = Entry->Next)
                new REFFEntryNode() { _name = Entry->Name, _offset = (int)Entry->DataOffset, _length = (int)Entry->DataLength }.Initialize(this, new DataSource((byte*)table->Address + Entry->DataOffset, (int)Entry->DataLength));
        }
        int tableLen = 0;
        protected override int OnCalculateSize(bool force)
        {
            int size = 0x60;
            tableLen = 0x9;
            foreach (ResourceNode n in Children)
            {
                tableLen += n.Name.Length + 11;
                size += n.CalculateSize(force);
            }
            return size + (tableLen = tableLen.Align(4));
        }
        protected internal override void OnRebuild(VoidPtr address, int length, bool force)
        {
            REFF* header = (REFF*)address;
            header->_linkPrev = 0;
            header->_linkNext = 0;
            header->_padding = 0;
            header->_dataLength = length - 0x18;
            header->_dataOffset = 0x48;
            header->_header._tag = header->_tag = REFF.Tag;
            header->_header._endian = -2;
            header->_header._version = 0x0700;
            header->_header._firstOffset = 0x10;
            header->_header._numEntries = 1;
            header->IdString = Name;

            REFTypeObjectTable* table = (REFTypeObjectTable*)((byte*)header + header->_dataOffset + 0x18);
            table->_entries = (short)Children.Count;
            table->_unk1 = 0;
            table->_length = tableLen;

            REFTypeObjectEntry* entry = table->First;
            int offset = tableLen;
            foreach (ResourceNode n in Children)
            {
                entry->Name = n.Name;
                entry->DataOffset = offset;
                entry->DataLength = n._calcSize - 0x20;
                n.Rebuild((VoidPtr)table + offset, n._calcSize, force);
                offset += n._calcSize;
                entry = entry->Next;
            }
        }

        internal static ResourceNode TryParse(DataSource source) { return ((REFF*)source.Address)->_tag == REFF.Tag ? new REFFNode() : null; }
    }
    public unsafe class REFFEntryNode : ResourceNode
    {
        internal REFFDataHeader* Header { get { return (REFFDataHeader*)WorkingUncompressed.Address; } }

        [Category("REFF Entry")]
        public int REFFOffset { get { return _offset; } }
        [Category("REFF Entry")]
        public int DataLength { get { return _length; } }

        public int _offset;
        public int _length;

        protected override bool OnInitialize()
        {
            base.OnInitialize();

            return true;
        }

        protected override void OnPopulate()
        {
            new REFFEmitterNode().Initialize(this, (VoidPtr)Header + 8, (int)Header->_headerSize);
            new REFFParticleNode().Initialize(this, (VoidPtr)Header->_params, (int)Header->_params->headersize);
            new REFFAnimationListNode()
            {
                _ptclTrackCount = *Header->_ptclTrackCount,
                _ptclInitTrackCount = *Header->_ptclInitTrackCount,
                _emitTrackCount = *Header->_emitTrackCount,
                _emitInitTrackCount = *Header->_emitInitTrackCount,
                _ptclTrackAddr = Header->_ptclTrack,
                _emitTrackAddr = Header->_emitTrack,
            }
            .Initialize(this, Header->_postFieldInfo, WorkingUncompressed.Length - ((int)Header->_postFieldInfo - (int)Header));
        }

        protected override int OnCalculateSize(bool force)
        {
            int size = 8;
            foreach (ResourceNode r in Children)
                size += r.CalculateSize(true);
            return size;
        }

        protected internal override void OnRebuild(VoidPtr address, int length, bool force)
        {
            base.OnRebuild(address, length, force);
        }
    }

    public unsafe class REFFAnimationListNode : ResourceNode
    {
        internal VoidPtr First { get { return (VoidPtr)WorkingUncompressed.Address; } }
        public short _ptclTrackCount, _ptclInitTrackCount, _emitTrackCount, _emitInitTrackCount;
        public buint* _ptclTrackAddr, _emitTrackAddr;
        public List<uint> _ptclTrack, _emitTrack;

        [Category("Post Field Info Table")]
        public short PtclTrackCount { get { return _ptclTrackCount; } }
        [Category("Post Field Info Table")]
        public short PtclInitTrackCount { get { return _ptclInitTrackCount; } }
        [Category("Post Field Info Table")]
        public short EmitTrackCount { get { return _emitTrackCount; } }
        [Category("Post Field Info Table")]
        public short EmitInitTrackCount { get { return _emitInitTrackCount; } }

        protected override bool OnInitialize()
        {
            _name = "Animations";

            return PtclTrackCount > 0 || EmitTrackCount > 0;
        }

        protected override void OnPopulate()
        {
            int offset = 0;
            buint* addr = _ptclTrackAddr;
            addr += PtclTrackCount; //skip nulled pointers to size list
            for (int i = 0; i < PtclTrackCount; i++)
            {
                new REFFAnimationNode().Initialize(this, First + offset, (int)*addr);
                offset += (int)*addr++;
            }
            addr = _emitTrackAddr;
            addr += EmitTrackCount; //skip nulled pointers to size list
            for (int i = 0; i < EmitTrackCount; i++)
            {
                new REFFAnimationNode().Initialize(this, First + offset, (int)*addr);
                offset += (int)*addr++;
            }
        }
    }
    public unsafe class REFFAnimationNode : ResourceNode
    {
        internal AnimCurveHeader* Header { get { return (AnimCurveHeader*)WorkingUncompressed.Address; } }

        AnimCurveHeader hdr;

        [Category("Animation")]
        public byte Magic { get { return hdr.magic; } }
        [Category("Animation")]
        public AnimCurveTarget KindType { get { return (AnimCurveTarget)hdr.kindType; } }
        [Category("Animation")]
        public AnimCurveType CurveFlag { get { return (AnimCurveType)hdr.curveFlag; } }
        [Category("Animation")]
        public byte KindEnable { get { return hdr.kindEnable; } }
        [Category("Animation")]
        public AnimCurveHeaderProcessFlagType ProcessFlag { get { return (AnimCurveHeaderProcessFlagType)hdr.processFlag; } }
        [Category("Animation")]
        public byte LoopCount { get { return hdr.loopCount; } }

        [Category("Animation")]
        public ushort RandomSeed { get { return hdr.randomSeed; } }
        [Category("Animation")]
        public ushort FrameLength { get { return hdr.frameLength; } }
        [Category("Animation")]
        public ushort Padding { get { return hdr.padding; } }

        [Category("Animation")]
        public uint KeyTableSize { get { return hdr.keyTable; } }
        [Category("Animation")]
        public uint RangeTableSize { get { return hdr.rangeTable; } }
        [Category("Animation")]
        public uint RandomTableSize { get { return hdr.randomTable; } }
        [Category("Animation")]
        public uint NameTableSize { get { return hdr.nameTable; } }
        [Category("Animation")]
        public uint InfoTableSize { get { return hdr.infoTable; } }

        Random random = null;

        protected override bool OnInitialize()
        {
            hdr = *Header;
            _name = "AnimCurve" + Index;
            random = new Random(RandomSeed);
            //if (CurveFlag == AnimCurveType.EmitterFloat || CurveFlag == AnimCurveType.Field || CurveFlag == AnimCurveType.PostField)
            //    MessageBox.Show(TreePath);
            return KeyTableSize > 4 || RangeTableSize > 4 || RandomTableSize > 4 || NameTableSize > 4 || InfoTableSize > 4;
        }

        protected override void OnPopulate()
        {
            if (KeyTableSize > 4)
                new REFFAnimCurveTableNode() { _name = "Key Table" }.Initialize(this, (VoidPtr)Header + 0x20, (int)KeyTableSize);
            if (RangeTableSize > 4)
                new REFFAnimCurveTableNode() { _name = "Range Table" }.Initialize(this, (VoidPtr)Header + 0x20 + KeyTableSize, (int)RangeTableSize);
            if (RandomTableSize > 4)
                new REFFAnimCurveTableNode() { _name = "Random Table" }.Initialize(this, (VoidPtr)Header + 0x20 + KeyTableSize + RangeTableSize, (int)RandomTableSize);
            if (NameTableSize > 4)
                new REFFAnimCurveNameTableNode() { _name = "Name Table" }.Initialize(this, (VoidPtr)Header + 0x20 + KeyTableSize + RangeTableSize + RandomTableSize, (int)NameTableSize);
            if (InfoTableSize > 4)
                new REFFAnimCurveTableNode() { _name = "Info Table" }.Initialize(this, (VoidPtr)Header + 0x20 + KeyTableSize + RangeTableSize + RandomTableSize + NameTableSize, (int)InfoTableSize);
        }
    }

    public unsafe class REFFAnimCurveNameTableNode : ResourceNode
    {
        internal AnimCurveTableHeader* Header { get { return (AnimCurveTableHeader*)WorkingUncompressed.Address; } }

        [Category("Name Table")]
        public string[] Names { get { return _names.ToArray(); } set { _names = value.ToList<string>(); SignalPropertyChange(); } }
        public List<string> _names = new List<string>();

        protected override bool OnInitialize()
        {
            _name = "Name Table";
            _names = new List<string>();
            bushort* addr = (bushort*)((VoidPtr)Header + 4 + Header->count * 4);
            for (int i = 0; i < Header->count; i++)
            {
                _names.Add(new String((sbyte*)addr + 2));
                addr = (bushort*)((VoidPtr)addr + 2 + *addr);
            }

            return false;
        }
    }

    public unsafe class REFFAnimCurveTableNode : ResourceNode
    {
        internal AnimCurveTableHeader* Header { get { return (AnimCurveTableHeader*)WorkingUncompressed.Address; } }

        [Category("AnimCurve Table")]
        public int Size { get { return WorkingUncompressed.Length; } }
        [Category("AnimCurve Table")]
        public int Count { get { return Header->count; } }
        [Category("AnimCurve Table")]
        public int Pad { get { return Header->pad; } }
        
        protected override bool OnInitialize()
        {
            if (_name == null)
                _name = "Table" + Index;
            return Count > 0;
        }

        protected override void OnPopulate()
        {
            VoidPtr addr = (VoidPtr)Header + 4;
            int s = (WorkingUncompressed.Length - 4) / Count;
            for (int i = 0; i < Count; i++)
                new MoveDefSectionParamNode() { _name = "Entry" + i }.Initialize(this, (VoidPtr)Header + 4 + i * s, s);
        }
    }

    public unsafe class REFFPostFieldInfoNode : ResourceNode
    {
        internal PostFieldInfo* Header { get { return (PostFieldInfo*)WorkingUncompressed.Address; } }

        PostFieldInfo hdr;

        [Category("Post Field Info")]
        public Vector3 Scale { get { return hdr.mAnimatableParams.mSize; } }
        [Category("Post Field Info")]
        public Vector3 Rotation { get { return hdr.mAnimatableParams.mRotate; } }
        [Category("Post Field Info")]
        public Vector3 Translation { get { return hdr.mAnimatableParams.mTranslate; } }
        [Category("Post Field Info")]
        public float ReferenceSpeed { get { return hdr.mReferenceSpeed; } }
        [Category("Post Field Info")]
        public PostFieldInfo.ControlSpeedType ControlSpeedType { get { return (PostFieldInfo.ControlSpeedType)hdr.mControlSpeedType; } }
        [Category("Post Field Info")]
        public PostFieldInfo.CollisionShapeType CollisionShapeType { get { return (PostFieldInfo.CollisionShapeType)hdr.mCollisionShapeType; } }
        [Category("Post Field Info")]
        public PostFieldInfo.ShapeOption ShapeOption { get { return CollisionShapeType == PostFieldInfo.CollisionShapeType.Sphere || CollisionShapeType == PostFieldInfo.CollisionShapeType.Plane ? (PostFieldInfo.ShapeOption)(((int)CollisionShapeType << 2) | hdr.mCollisionShapeOption) : PostFieldInfo.ShapeOption.None; } }
        [Category("Post Field Info")]
        public PostFieldInfo.CollisionType CollisionType { get { return (PostFieldInfo.CollisionType)hdr.mCollisionType; } }
        [Category("Post Field Info")]
        public PostFieldInfo.CollisionOption CollisionOption { get { return (PostFieldInfo.CollisionOption)(short)hdr.mCollisionOption; } }
        [Category("Post Field Info")]
        public ushort StartFrame { get { return hdr.mStartFrame; } }
        [Category("Post Field Info")]
        public Vector3 SpeedFactor { get { return hdr.mSpeedFactor; } }

        protected override bool OnInitialize()
        {
            _name = "Entry" + Index;
            hdr = *Header;
            return false;
        }

        protected override void OnPopulate()
        {
            base.OnPopulate();
        }
    }

    public unsafe class REFFParticleNode : ResourceNode
    {
        internal ParticleParameterHeader* Params { get { return (ParticleParameterHeader*)WorkingUncompressed.Address; } }

        ParticleParameterHeader hdr;
        ParticleParameterDesc desc;

        //[Category("Particle Parameters")]
        //public uint HeaderSize { get { return hdr.headersize; } }

        [Category("Particle Parameters"), TypeConverter(typeof(RGBAStringConverter))]
        public RGBAPixel mColor11 { get { return desc.mColor11; } set { desc.mColor11 = value; SignalPropertyChange(); } }
        [Category("Particle Parameters"), TypeConverter(typeof(RGBAStringConverter))]
        public RGBAPixel mColor12 { get { return desc.mColor12; } set { desc.mColor12 = value; SignalPropertyChange(); } }
        [Category("Particle Parameters"), TypeConverter(typeof(RGBAStringConverter))]
        public RGBAPixel mColor21 { get { return desc.mColor21; } set { desc.mColor21 = value; SignalPropertyChange(); } }
        [Category("Particle Parameters"), TypeConverter(typeof(RGBAStringConverter))]
        public RGBAPixel mColor22 { get { return desc.mColor22; } set { desc.mColor22 = value; SignalPropertyChange(); } }

        [Category("Particle Parameters"), TypeConverter(typeof(Vector2StringConverter))]
        public Vector2 Size { get { return desc.size; } set { desc.size = value; SignalPropertyChange(); } }
        [Category("Particle Parameters"), TypeConverter(typeof(Vector2StringConverter))]
        public Vector2 Scale { get { return desc.scale; } set { desc.scale = value; SignalPropertyChange(); } }
        [Category("Particle Parameters"), TypeConverter(typeof(Vector3StringConverter))]
        public Vector3 Rotate { get { return desc.rotate; } set { desc.rotate = value; SignalPropertyChange(); } }

        [Category("Particle Parameters"), TypeConverter(typeof(Vector2StringConverter))]
        public Vector2 TextureScale1 { get { return desc.textureScale1; } set { desc.textureScale1 = value; SignalPropertyChange(); } }
        [Category("Particle Parameters"), TypeConverter(typeof(Vector2StringConverter))]
        public Vector2 TextureScale2 { get { return desc.textureScale2; } set { desc.textureScale2 = value; SignalPropertyChange(); } }
        [Category("Particle Parameters"), TypeConverter(typeof(Vector2StringConverter))]
        public Vector2 TextureScale3 { get { return desc.textureScale3; } set { desc.textureScale3 = value; SignalPropertyChange(); } }

        [Category("Particle Parameters"), TypeConverter(typeof(Vector3StringConverter))]
        public Vector3 TextureRotate { get { return desc.textureRotate; } set { desc.textureRotate = value; SignalPropertyChange(); } }

        [Category("Particle Parameters"), TypeConverter(typeof(Vector2StringConverter))]
        public Vector2 TextureTranslate1 { get { return desc.textureTranslate1; } set { desc.textureTranslate1 = value; SignalPropertyChange(); } }
        [Category("Particle Parameters"), TypeConverter(typeof(Vector2StringConverter))]
        public Vector2 TextureTranslate2 { get { return desc.textureTranslate2; } set { desc.textureTranslate2 = value; SignalPropertyChange(); } }
        [Category("Particle Parameters"), TypeConverter(typeof(Vector2StringConverter))]
        public Vector2 TextureTranslate3 { get { return desc.textureTranslate3; } set { desc.textureTranslate3 = value; SignalPropertyChange(); } }

        [Category("Particle Parameters")]
        public ushort TextureWrap { get { return desc.textureWrap; } set { desc.textureWrap = value; SignalPropertyChange(); } }
        [Category("Particle Parameters")]
        public byte TextureReverse { get { return desc.textureReverse; } set { desc.textureReverse = value; SignalPropertyChange(); } }

        [Category("Particle Parameters")]
        public byte mACmpRef0 { get { return desc.mACmpRef0; } set { desc.mACmpRef0 = value; SignalPropertyChange(); } }
        [Category("Particle Parameters")]
        public byte mACmpRef1 { get { return desc.mACmpRef1; } set { desc.mACmpRef1 = value; SignalPropertyChange(); } }

        [Category("Particle Parameters")]
        public byte RotateOffsetRandom1 { get { return desc.rotateOffsetRandomX; } set { desc.rotateOffsetRandomX = value; SignalPropertyChange(); } }
        [Category("Particle Parameters")]
        public byte RotateOffsetRandom2 { get { return desc.rotateOffsetRandomY; } set { desc.rotateOffsetRandomY = value; SignalPropertyChange(); } }
        [Category("Particle Parameters")]
        public byte RotateOffsetRandom3 { get { return desc.rotateOffsetRandomZ; } set { desc.rotateOffsetRandomZ = value; SignalPropertyChange(); } }

        [Category("Particle Parameters"), TypeConverter(typeof(Vector3StringConverter))]
        public Vector3 RotateOffset { get { return desc.rotateOffset; } set { desc.rotateOffset = value; SignalPropertyChange(); } }

        [Category("Particle Parameters")]
        public string Texture1Name { get { return _textureNames[0]; } set { _textureNames[0] = value; SignalPropertyChange(); } }
        [Category("Particle Parameters")]
        public string Texture2Name { get { return _textureNames[1]; } set { _textureNames[1] = value; SignalPropertyChange(); } }
        [Category("Particle Parameters")]
        public string Texture3Name { get { return _textureNames[2]; } set { _textureNames[2] = value; SignalPropertyChange(); } }

        public List<string> _textureNames = new List<string>(3);

        protected override bool OnInitialize()
        {
            _name = "Particle";
            hdr = *Params;
            desc = hdr.paramDesc;

            VoidPtr addr = Params->paramDesc.textureNames.Address;
            for (int i = 0; i < 3; i++)
            {
                if (*(bushort*)addr > 1)
                    _textureNames.Add(new String((sbyte*)(addr + 2)));
                else
                    _textureNames.Add(null);
                addr += 2 + *(bushort*)addr;
            }

            return false;
        }
    }
    public unsafe class REFFEmitterNode : ResourceNode
    {
        internal EmitterDesc* Descriptor { get { return (EmitterDesc*)WorkingUncompressed.Address; } }

        EmitterDesc desc;

        [Category("Emitter Descriptor")]
        public EmitterDesc.EmitterCommonFlag CommonFlag { get { return (EmitterDesc.EmitterCommonFlag)(uint)desc.commonFlag; } set { desc.commonFlag = (uint)value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public uint emitFlag { get { return desc.emitFlag; } set { desc.emitFlag = value; SignalPropertyChange(); } } // EmitFormType - value & 0xFF
        [Category("Emitter Descriptor")]
        public ushort emitLife { get { return desc.emitLife; } set { desc.emitLife = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public ushort ptclLife { get { return desc.ptclLife; } set { desc.ptclLife = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public sbyte ptclLifeRandom { get { return desc.ptclLifeRandom; } set { desc.ptclLifeRandom = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public sbyte inheritChildPtclTranslate { get { return desc.inheritChildPtclTranslate; } set { desc.inheritChildPtclTranslate = value; SignalPropertyChange(); } }

        [Category("Emitter Descriptor")]
        public sbyte emitEmitIntervalRandom { get { return desc.emitEmitIntervalRandom; } set { desc.emitEmitIntervalRandom = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public sbyte emitEmitRandom { get { return desc.emitEmitRandom; } set { desc.emitEmitRandom = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public float emitEmit { get { return desc.emitEmit; } set { desc.emitEmit = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public ushort emitEmitStart { get { return desc.emitEmitStart; } set { desc.emitEmitStart = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public ushort emitEmitPast { get { return desc.emitEmitPast; } set { desc.emitEmitPast = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public ushort emitEmitInterval { get { return desc.emitEmitInterval; } set { desc.emitEmitInterval = value; SignalPropertyChange(); } }

        [Category("Emitter Descriptor")]
        public sbyte inheritPtclTranslate { get { return desc.inheritPtclTranslate; } set { desc.inheritPtclTranslate = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public sbyte inheritChildEmitTranslate { get { return desc.inheritChildEmitTranslate; } set { desc.inheritChildEmitTranslate = value; SignalPropertyChange(); } }

        [Category("Emitter Descriptor")]
        public float commonParam1 { get { return desc.commonParam1; } set { desc.commonParam1 = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public float commonParam2 { get { return desc.commonParam2; } set { desc.commonParam2 = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public float commonParam3 { get { return desc.commonParam3; } set { desc.commonParam3 = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public float commonParam4 { get { return desc.commonParam4; } set { desc.commonParam4 = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public float commonParam5 { get { return desc.commonParam5; } set { desc.commonParam5 = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public float commonParam6 { get { return desc.commonParam6; } set { desc.commonParam6 = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public ushort emitEmitDiv { get { return desc.emitEmitDiv; } set { desc.emitEmitDiv = value; SignalPropertyChange(); } } //aka orig tick

        [Category("Emitter Descriptor")]
        public sbyte velInitVelocityRandom { get { return desc.velInitVelocityRandom; } set { desc.velInitVelocityRandom = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public sbyte velMomentumRandom { get { return desc.velMomentumRandom; } set { desc.velMomentumRandom = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public float velPowerRadiationDir { get { return desc.velPowerRadiationDir; } set { desc.velPowerRadiationDir = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public float velPowerYAxis { get { return desc.velPowerYAxis; } set { desc.velPowerYAxis = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public float velPowerRandomDir { get { return desc.velPowerRandomDir; } set { desc.velPowerRandomDir = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public float velPowerNormalDir { get { return desc.velPowerNormalDir; } set { desc.velPowerNormalDir = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public float velDiffusionEmitterNormal { get { return desc.velDiffusionEmitterNormal; } set { desc.velDiffusionEmitterNormal = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public float velPowerSpecDir { get { return desc.velPowerSpecDir; } set { desc.velPowerSpecDir = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public float velDiffusionSpecDir { get { return desc.velDiffusionSpecDir; } set { desc.velDiffusionSpecDir = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor"), TypeConverter(typeof(Vector3StringConverter))]
        public Vector3 velSpecDir { get { return desc.velSpecDir; } set { desc.velSpecDir = value; SignalPropertyChange(); } }

        [Category("Emitter Descriptor"), TypeConverter(typeof(Vector3StringConverter))]
        public Vector3 scale { get { return desc.scale; } set { desc.scale = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor"), TypeConverter(typeof(Vector3StringConverter))]
        public Vector3 rotate { get { return desc.rotate; } set { desc.rotate = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor"), TypeConverter(typeof(Vector3StringConverter))]
        public Vector3 translate { get { return desc.translate; } set { desc.translate = value; SignalPropertyChange(); } }

        [Category("Emitter Descriptor")]
        public byte lodNear { get { return desc.lodNear; } set { desc.lodNear = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public byte lodFar { get { return desc.lodFar; } set { desc.lodFar = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public byte lodMinEmit { get { return desc.lodMinEmit; } set { desc.lodMinEmit = value; SignalPropertyChange(); } }
        [Category("Emitter Descriptor")]
        public byte lodAlpha { get { return desc.lodAlpha; } set { desc.lodAlpha = value; SignalPropertyChange(); } }

        [Category("Emitter Descriptor")]
        public uint randomSeed { get { return desc.randomSeed; } set { desc.randomSeed = value; SignalPropertyChange(); } }

        //[Category("Emitter Descriptor")]
        //public byte userdata1 { get { fixed (byte* dat = desc.userdata) return dat[0]; } set { fixed (byte* dat = desc.userdata) dat[0] = value; SignalPropertyChange(); } }
        //[Category("Emitter Descriptor")]
        //public byte userdata2 { get { fixed (byte* dat = desc.userdata) return dat[1]; } set { fixed (byte* dat = desc.userdata) dat[1] = value; SignalPropertyChange(); } }
        //[Category("Emitter Descriptor")]
        //public byte userdata3 { get { fixed (byte* dat = desc.userdata) return dat[2]; } set { fixed (byte* dat = desc.userdata) dat[2] = value; SignalPropertyChange(); } }
        //[Category("Emitter Descriptor")]
        //public byte userdata4 { get { fixed (byte* dat = desc.userdata) return dat[3]; } set { fixed (byte* dat = desc.userdata) dat[3] = value; SignalPropertyChange(); } }
        //[Category("Emitter Descriptor")]
        //public byte userdata5 { get { fixed (byte* dat = desc.userdata) return dat[4]; } set { fixed (byte* dat = desc.userdata) dat[4] = value; SignalPropertyChange(); } }
        //[Category("Emitter Descriptor")]
        //public byte userdata6 { get { fixed (byte* dat = desc.userdata) return dat[5]; } set { fixed (byte* dat = desc.userdata) dat[5] = value; SignalPropertyChange(); } }
        //[Category("Emitter Descriptor")]
        //public byte userdata7 { get { fixed (byte* dat = desc.userdata) return dat[6]; } set { fixed (byte* dat = desc.userdata) dat[6] = value; SignalPropertyChange(); } }
        //[Category("Emitter Descriptor")]
        //public byte userdata8 { get { fixed (byte* dat = desc.userdata) return dat[7]; } set { fixed (byte* dat = desc.userdata) dat[7] = value; SignalPropertyChange(); } }

        #region Draw Settings

        public EmitterDrawSetting.DrawFlag mFlags { get { return (EmitterDrawSetting.DrawFlag)(ushort)drawSetting.mFlags; } set { drawSetting.mFlags = (ushort)value; SignalPropertyChange(); } }

        public byte mACmpComp0 { get { return drawSetting.mACmpComp0; } set { drawSetting.mACmpComp0 = value; SignalPropertyChange(); } }
        public byte mACmpComp1 { get { return drawSetting.mACmpComp1; } set { drawSetting.mACmpComp1 = value; SignalPropertyChange(); } }
        public byte mACmpOp { get { return drawSetting.mACmpOp; } set { drawSetting.mACmpOp = value; SignalPropertyChange(); } }

        public byte mNumTevs { get { return drawSetting.mNumTevs; } set { drawSetting.mNumTevs = value; SignalPropertyChange(); } }
        public byte mFlagClamp { get { return drawSetting.mFlagClamp; } set { drawSetting.mFlagClamp = value; SignalPropertyChange(); } }

        public EmitterDrawSetting.IndirectTargetStage mIndirectTargetStage { get { return (EmitterDrawSetting.IndirectTargetStage)drawSetting.mIndirectTargetStage; } set { drawSetting.mIndirectTargetStage = (byte)value; SignalPropertyChange(); } }

        //public byte mTevTexture1 { get { return drawSetting.mTevTexture1; } set { drawSetting.mTevTexture1 = value; SignalPropertyChange(); } }
        //public byte mTevTexture2 { get { return drawSetting.mTevTexture2; } set { drawSetting.mTevTexture2 = value; SignalPropertyChange(); } }
        //public byte mTevTexture3 { get { return drawSetting.mTevTexture3; } set { drawSetting.mTevTexture3 = value; SignalPropertyChange(); } }
        //public byte mTevTexture4 { get { return drawSetting.mTevTexture4; } set { drawSetting.mTevTexture4 = value; SignalPropertyChange(); } }

        //#region Color

        //[Category("TEV Color 1")]
        //public ColorArg c1mA { get { return (ColorArg)drawSetting.mTevColor1.mA; } set { drawSetting.mTevColor1.mA = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 1")]
        //public ColorArg c1mB { get { return (ColorArg)drawSetting.mTevColor1.mB; } set { drawSetting.mTevColor1.mB = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 1")]
        //public ColorArg c1mC { get { return (ColorArg)drawSetting.mTevColor1.mC; } set { drawSetting.mTevColor1.mC = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 1")]
        //public ColorArg c1mD { get { return (ColorArg)drawSetting.mTevColor1.mD; } set { drawSetting.mTevColor1.mD = (byte)value; SignalPropertyChange(); } }

        //[Category("TEV Color 2")]
        //public ColorArg c2mA { get { return (ColorArg)drawSetting.mTevColor2.mA; } set { drawSetting.mTevColor2.mA = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 2")]
        //public ColorArg c2mB { get { return (ColorArg)drawSetting.mTevColor2.mB; } set { drawSetting.mTevColor2.mB = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 2")]
        //public ColorArg c2mC { get { return (ColorArg)drawSetting.mTevColor2.mC; } set { drawSetting.mTevColor2.mC = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 2")]
        //public ColorArg c2mD { get { return (ColorArg)drawSetting.mTevColor2.mD; } set { drawSetting.mTevColor2.mD = (byte)value; SignalPropertyChange(); } }

        //[Category("TEV Color 3")]
        //public ColorArg c3mA { get { return (ColorArg)drawSetting.mTevColor3.mA; } set { drawSetting.mTevColor3.mA = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 3")]
        //public ColorArg c3mB { get { return (ColorArg)drawSetting.mTevColor3.mB; } set { drawSetting.mTevColor3.mB = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 3")]
        //public ColorArg c3mC { get { return (ColorArg)drawSetting.mTevColor3.mC; } set { drawSetting.mTevColor3.mC = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 3")]
        //public ColorArg c3mD { get { return (ColorArg)drawSetting.mTevColor3.mD; } set { drawSetting.mTevColor3.mD = (byte)value; SignalPropertyChange(); } }

        //[Category("TEV Color 4")]
        //public ColorArg c4mA { get { return (ColorArg)drawSetting.mTevColor4.mA; } set { drawSetting.mTevColor4.mA = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 4")]
        //public ColorArg c4mB { get { return (ColorArg)drawSetting.mTevColor4.mB; } set { drawSetting.mTevColor4.mB = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 4")]
        //public ColorArg c4mC { get { return (ColorArg)drawSetting.mTevColor4.mC; } set { drawSetting.mTevColor4.mC = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 4")]
        //public ColorArg c4mD { get { return (ColorArg)drawSetting.mTevColor4.mD; } set { drawSetting.mTevColor4.mD = (byte)value; SignalPropertyChange(); } }

        //[Category("TEV Color 1 Operation")]
        //public TevOp c1mOp { get { return (TevOp)drawSetting.mTevColorOp1.mOp; } set { drawSetting.mTevColorOp1.mOp = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 1 Operation")]
        //public Bias c1mBias { get { return (Bias)drawSetting.mTevColorOp1.mBias; } set { drawSetting.mTevColorOp1.mBias = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 1 Operation")]
        //public TevScale c1mScale { get { return (TevScale)drawSetting.mTevColorOp1.mScale; } set { drawSetting.mTevColorOp1.mScale = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 1 Operation")]
        //public bool c1mClamp { get { return drawSetting.mTevColorOp1.mClamp != 0; } set { drawSetting.mTevColorOp1.mClamp = (byte)(value ? 1 : 0); SignalPropertyChange(); } }
        //[Category("TEV Color 1 Operation")]
        //public TevRegID c1mOutReg { get { return (TevRegID)drawSetting.mTevColorOp1.mOutReg; } set { drawSetting.mTevColorOp1.mOutReg = (byte)value; SignalPropertyChange(); } }

        //[Category("TEV Color 2 Operation")]
        //public TevOp c2mOp { get { return (TevOp)drawSetting.mTevColorOp2.mOp; } set { drawSetting.mTevColorOp2.mOp = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 2 Operation")]
        //public Bias c2mBias { get { return (Bias)drawSetting.mTevColorOp2.mBias; } set { drawSetting.mTevColorOp2.mBias = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 2 Operation")]
        //public TevScale c2mScale { get { return (TevScale)drawSetting.mTevColorOp2.mScale; } set { drawSetting.mTevColorOp2.mScale = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 2 Operation")]
        //public bool c2mClamp { get { return drawSetting.mTevColorOp2.mClamp != 0; } set { drawSetting.mTevColorOp2.mClamp = (byte)(value ? 1 : 0); SignalPropertyChange(); } }
        //[Category("TEV Color 2 Operation")]
        //public TevRegID c2mOutReg { get { return (TevRegID)drawSetting.mTevColorOp2.mOutReg; } set { drawSetting.mTevColorOp2.mOutReg = (byte)value; SignalPropertyChange(); } }

        //[Category("TEV Color 3 Operation")]
        //public TevOp c3mOp { get { return (TevOp)drawSetting.mTevColorOp3.mOp; } set { drawSetting.mTevColorOp3.mOp = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 3 Operation")]
        //public Bias c3mBias { get { return (Bias)drawSetting.mTevColorOp3.mBias; } set { drawSetting.mTevColorOp3.mBias = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 3 Operation")]
        //public TevScale c3mScale { get { return (TevScale)drawSetting.mTevColorOp3.mScale; } set { drawSetting.mTevColorOp3.mScale = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 3 Operation")]
        //public bool c3mClamp { get { return drawSetting.mTevColorOp3.mClamp != 0; } set { drawSetting.mTevColorOp3.mClamp = (byte)(value ? 1 : 0); SignalPropertyChange(); } }
        //[Category("TEV Color 3 Operation")]
        //public TevRegID c3mOutReg { get { return (TevRegID)drawSetting.mTevColorOp3.mOutReg; } set { drawSetting.mTevColorOp3.mOutReg = (byte)value; SignalPropertyChange(); } }

        //[Category("TEV Color 4 Operation")]
        //public TevOp c4mOp { get { return (TevOp)drawSetting.mTevColorOp4.mOp; } set { drawSetting.mTevColorOp4.mOp = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 4 Operation")]
        //public Bias c4mBias { get { return (Bias)drawSetting.mTevColorOp4.mBias; } set { drawSetting.mTevColorOp4.mBias = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 4 Operation")]
        //public TevScale c4mScale { get { return (TevScale)drawSetting.mTevColorOp4.mScale; } set { drawSetting.mTevColorOp4.mScale = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Color 4 Operation")]
        //public bool c4mClamp { get { return drawSetting.mTevColorOp4.mClamp != 0; } set { drawSetting.mTevColorOp4.mClamp = (byte)(value ? 1 : 0); SignalPropertyChange(); } }
        //[Category("TEV Color 4 Operation")]
        //public TevRegID c4mOutReg { get { return (TevRegID)drawSetting.mTevColorOp4.mOutReg; } set { drawSetting.mTevColorOp4.mOutReg = (byte)value; SignalPropertyChange(); } }
        
        //#endregion  

        //#region Alpha

        //[Category("TEV Alpha 1")]
        //public ColorArg a1mA { get { return (ColorArg)drawSetting.mTevAlpha1.mA; } set { drawSetting.mTevAlpha1.mA = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 1")]
        //public ColorArg a1mB { get { return (ColorArg)drawSetting.mTevAlpha1.mB; } set { drawSetting.mTevAlpha1.mB = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 1")]
        //public ColorArg a1mC { get { return (ColorArg)drawSetting.mTevAlpha1.mC; } set { drawSetting.mTevAlpha1.mC = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 1")]
        //public ColorArg a1mD { get { return (ColorArg)drawSetting.mTevAlpha1.mD; } set { drawSetting.mTevAlpha1.mD = (byte)value; SignalPropertyChange(); } }

        //[Category("TEV Alpha 2")]
        //public ColorArg a2mA { get { return (ColorArg)drawSetting.mTevAlpha2.mA; } set { drawSetting.mTevAlpha2.mA = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 2")]
        //public ColorArg a2mB { get { return (ColorArg)drawSetting.mTevAlpha2.mB; } set { drawSetting.mTevAlpha2.mB = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 2")]
        //public ColorArg a2mC { get { return (ColorArg)drawSetting.mTevAlpha2.mC; } set { drawSetting.mTevAlpha2.mC = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 2")]
        //public ColorArg a2mD { get { return (ColorArg)drawSetting.mTevAlpha2.mD; } set { drawSetting.mTevAlpha2.mD = (byte)value; SignalPropertyChange(); } }

        //[Category("TEV Alpha 3")]
        //public ColorArg a3mA { get { return (ColorArg)drawSetting.mTevAlpha3.mA; } set { drawSetting.mTevAlpha3.mA = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 3")]
        //public ColorArg a3mB { get { return (ColorArg)drawSetting.mTevAlpha3.mB; } set { drawSetting.mTevAlpha3.mB = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 3")]
        //public ColorArg a3mC { get { return (ColorArg)drawSetting.mTevAlpha3.mC; } set { drawSetting.mTevAlpha3.mC = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 3")]
        //public ColorArg a3mD { get { return (ColorArg)drawSetting.mTevAlpha3.mD; } set { drawSetting.mTevAlpha3.mD = (byte)value; SignalPropertyChange(); } }

        //[Category("TEV Alpha 4")]
        //public ColorArg a4mA { get { return (ColorArg)drawSetting.mTevAlpha4.mA; } set { drawSetting.mTevAlpha4.mA = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 4")]
        //public ColorArg a4mB { get { return (ColorArg)drawSetting.mTevAlpha4.mB; } set { drawSetting.mTevAlpha4.mB = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 4")]
        //public ColorArg a4mC { get { return (ColorArg)drawSetting.mTevAlpha4.mC; } set { drawSetting.mTevAlpha4.mC = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 4")]
        //public ColorArg a4mD { get { return (ColorArg)drawSetting.mTevAlpha4.mD; } set { drawSetting.mTevAlpha4.mD = (byte)value; SignalPropertyChange(); } }

        //[Category("TEV Alpha 1 Operation")]
        //public TevOp a1mOp { get { return (TevOp)drawSetting.mTevAlphaOp1.mOp; } set { drawSetting.mTevAlphaOp1.mOp = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 1 Operation")]
        //public Bias a1mBias { get { return (Bias)drawSetting.mTevAlphaOp1.mBias; } set { drawSetting.mTevAlphaOp1.mBias = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 1 Operation")]
        //public TevScale a1mScale { get { return (TevScale)drawSetting.mTevAlphaOp1.mScale; } set { drawSetting.mTevAlphaOp1.mScale = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 1 Operation")]
        //public bool a1mClamp { get { return drawSetting.mTevAlphaOp1.mClamp != 0; } set { drawSetting.mTevAlphaOp1.mClamp = (byte)(value ? 1 : 0); SignalPropertyChange(); } }
        //[Category("TEV Alpha 1 Operation")]
        //public TevRegID a1mOutReg { get { return (TevRegID)drawSetting.mTevAlphaOp1.mOutReg; } set { drawSetting.mTevAlphaOp1.mOutReg = (byte)value; SignalPropertyChange(); } }

        //[Category("TEV Alpha 2 Operation")]
        //public TevOp a2mOp { get { return (TevOp)drawSetting.mTevAlphaOp2.mOp; } set { drawSetting.mTevAlphaOp2.mOp = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 2 Operation")]
        //public Bias a2mBias { get { return (Bias)drawSetting.mTevAlphaOp2.mBias; } set { drawSetting.mTevAlphaOp2.mBias = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 2 Operation")]
        //public TevScale a2mScale { get { return (TevScale)drawSetting.mTevAlphaOp2.mScale; } set { drawSetting.mTevAlphaOp2.mScale = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 2 Operation")]
        //public bool a2mClamp { get { return drawSetting.mTevAlphaOp2.mClamp != 0; } set { drawSetting.mTevAlphaOp2.mClamp = (byte)(value ? 1 : 0); SignalPropertyChange(); } }
        //[Category("TEV Alpha 2 Operation")]
        //public TevRegID a2mOutReg { get { return (TevRegID)drawSetting.mTevAlphaOp2.mOutReg; } set { drawSetting.mTevAlphaOp2.mOutReg = (byte)value; SignalPropertyChange(); } }

        //[Category("TEV Alpha 3 Operation")]
        //public TevOp a3mOp { get { return (TevOp)drawSetting.mTevAlphaOp3.mOp; } set { drawSetting.mTevAlphaOp3.mOp = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 3 Operation")]
        //public Bias a3mBias { get { return (Bias)drawSetting.mTevAlphaOp3.mBias; } set { drawSetting.mTevAlphaOp3.mBias = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 3 Operation")]
        //public TevScale a3mScale { get { return (TevScale)drawSetting.mTevAlphaOp3.mScale; } set { drawSetting.mTevAlphaOp3.mScale = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 3 Operation")]
        //public bool a3mClamp { get { return drawSetting.mTevAlphaOp3.mClamp != 0; } set { drawSetting.mTevAlphaOp3.mClamp = (byte)(value ? 1 : 0); SignalPropertyChange(); } }
        //[Category("TEV Alpha 3 Operation")]
        //public TevRegID a3mOutReg { get { return (TevRegID)drawSetting.mTevAlphaOp3.mOutReg; } set { drawSetting.mTevAlphaOp3.mOutReg = (byte)value; SignalPropertyChange(); } }

        //[Category("TEV Alpha 4 Operation")]
        //public TevOp a4mOp { get { return (TevOp)drawSetting.mTevAlphaOp4.mOp; } set { drawSetting.mTevAlphaOp4.mOp = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 4 Operation")]
        //public Bias a4mBias { get { return (Bias)drawSetting.mTevAlphaOp4.mBias; } set { drawSetting.mTevAlphaOp4.mBias = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 4 Operation")]
        //public TevScale a4mScale { get { return (TevScale)drawSetting.mTevAlphaOp4.mScale; } set { drawSetting.mTevAlphaOp4.mScale = (byte)value; SignalPropertyChange(); } }
        //[Category("TEV Alpha 4 Operation")]
        //public bool a4mClamp { get { return drawSetting.mTevAlphaOp4.mClamp != 0; } set { drawSetting.mTevAlphaOp4.mClamp = (byte)(value ? 1 : 0); SignalPropertyChange(); } }
        //[Category("TEV Alpha 4 Operation")]
        //public TevRegID a4mOutReg { get { return (TevRegID)drawSetting.mTevAlphaOp4.mOutReg; } set { drawSetting.mTevAlphaOp4.mOutReg = (byte)value; SignalPropertyChange(); } }

        //#endregion

        //[Category("Constant Register Selection")]
        //public TevKColorSel mTevKColorSel1 { get { return (TevKColorSel)drawSetting.mTevKColorSel1; } set { drawSetting.mTevKColorSel1 = (byte)value; SignalPropertyChange(); } }
        //[Category("Constant Register Selection")]
        //public TevKAlphaSel mTevKAlphaSel1 { get { return (TevKAlphaSel)drawSetting.mTevKAlphaSel1; } set { drawSetting.mTevKAlphaSel1 = (byte)value; SignalPropertyChange(); } }
        //[Category("Constant Register Selection")]
        //public TevKColorSel mTevKColorSel2 { get { return (TevKColorSel)drawSetting.mTevKColorSel2; } set { drawSetting.mTevKColorSel2 = (byte)value; SignalPropertyChange(); } }
        //[Category("Constant Register Selection")]
        //public TevKAlphaSel mTevKAlphaSel2 { get { return (TevKAlphaSel)drawSetting.mTevKAlphaSel2; } set { drawSetting.mTevKAlphaSel2 = (byte)value; SignalPropertyChange(); } }
        //[Category("Constant Register Selection")]
        //public TevKColorSel mTevKColorSel3 { get { return (TevKColorSel)drawSetting.mTevKColorSel3; } set { drawSetting.mTevKColorSel3 = (byte)value; SignalPropertyChange(); } }
        //[Category("Constant Register Selection")]
        //public TevKAlphaSel mTevKAlphaSel3 { get { return (TevKAlphaSel)drawSetting.mTevKAlphaSel3; } set { drawSetting.mTevKAlphaSel3 = (byte)value; SignalPropertyChange(); } }
        //[Category("Constant Register Selection")]
        //public TevKColorSel mTevKColorSel4 { get { return (TevKColorSel)drawSetting.mTevKColorSel4; } set { drawSetting.mTevKColorSel4 = (byte)value; SignalPropertyChange(); } }
        //[Category("Constant Register Selection")]
        //public TevKAlphaSel mTevKAlphaSel4 { get { return (TevKAlphaSel)drawSetting.mTevKAlphaSel4; } set { drawSetting.mTevKAlphaSel4 = (byte)value; SignalPropertyChange(); } }
        
        //BlendMode
        [Category("Blend Mode")]
        public GXBlendMode BlendType { get { return (GXBlendMode)drawSetting.mBlendMode.mType; } set { drawSetting.mBlendMode.mType = (byte)value; SignalPropertyChange(); } }
        [Category("Blend Mode")]
        public BlendFactor SrcFactor { get { return (BlendFactor)drawSetting.mBlendMode.mSrcFactor; } set { drawSetting.mBlendMode.mSrcFactor = (byte)value; SignalPropertyChange(); } }
        [Category("Blend Mode")]
        public BlendFactor DstFactor { get { return (BlendFactor)drawSetting.mBlendMode.mDstFactor; } set { drawSetting.mBlendMode.mDstFactor = (byte)value; SignalPropertyChange(); } }
        [Category("Blend Mode")]
        public GXLogicOp Operation { get { return (GXLogicOp)drawSetting.mBlendMode.mOp; } set { drawSetting.mBlendMode.mOp = (byte)value; SignalPropertyChange(); } }

        //Color
        [Category("Color Input")]
        public EmitterDrawSetting.ColorInput.RasColor cmRasColor { get { return (EmitterDrawSetting.ColorInput.RasColor)drawSetting.mColorInput.mRasColor; } set { drawSetting.mColorInput.mRasColor = (byte)value; SignalPropertyChange(); } }
        [Category("Color Input")]
        public EmitterDrawSetting.ColorInput.TevColor cmTevColor1 { get { return (EmitterDrawSetting.ColorInput.TevColor)drawSetting.mColorInput.mTevColor1; } set { drawSetting.mColorInput.mTevColor1 = (byte)value; SignalPropertyChange(); } }
        [Category("Color Input")]
        public EmitterDrawSetting.ColorInput.TevColor cmTevColor2 { get { return (EmitterDrawSetting.ColorInput.TevColor)drawSetting.mColorInput.mTevColor2; } set { drawSetting.mColorInput.mTevColor2 = (byte)value; SignalPropertyChange(); } }
        [Category("Color Input")]
        public EmitterDrawSetting.ColorInput.TevColor cmTevColor3 { get { return (EmitterDrawSetting.ColorInput.TevColor)drawSetting.mColorInput.mTevColor3; } set { drawSetting.mColorInput.mTevColor3 = (byte)value; SignalPropertyChange(); } }
        [Category("Color Input")]
        public EmitterDrawSetting.ColorInput.TevColor cmTevKColor1 { get { return (EmitterDrawSetting.ColorInput.TevColor)drawSetting.mColorInput.mTevKColor1; } set { drawSetting.mColorInput.mTevKColor1 = (byte)value; SignalPropertyChange(); } }
        [Category("Color Input")]
        public EmitterDrawSetting.ColorInput.TevColor cmTevKColor2 { get { return (EmitterDrawSetting.ColorInput.TevColor)drawSetting.mColorInput.mTevKColor2; } set { drawSetting.mColorInput.mTevKColor2 = (byte)value; SignalPropertyChange(); } }
        [Category("Color Input")]
        public EmitterDrawSetting.ColorInput.TevColor cmTevKColor3 { get { return (EmitterDrawSetting.ColorInput.TevColor)drawSetting.mColorInput.mTevKColor3; } set { drawSetting.mColorInput.mTevKColor3 = (byte)value; SignalPropertyChange(); } }
        [Category("Color Input")]
        public EmitterDrawSetting.ColorInput.TevColor cmTevKColor4 { get { return (EmitterDrawSetting.ColorInput.TevColor)drawSetting.mColorInput.mTevKColor4; } set { drawSetting.mColorInput.mTevKColor4 = (byte)value; SignalPropertyChange(); } }
        
        //Alpha
        [Category("Alpha Input")]
        public EmitterDrawSetting.ColorInput.RasColor amRasColor { get { return (EmitterDrawSetting.ColorInput.RasColor)drawSetting.mAlphaInput.mRasColor; } set { drawSetting.mAlphaInput.mRasColor = (byte)value; SignalPropertyChange(); } }
        [Category("Alpha Input")]
        public EmitterDrawSetting.ColorInput.TevColor amTevColor1 { get { return (EmitterDrawSetting.ColorInput.TevColor)drawSetting.mAlphaInput.mTevColor1; } set { drawSetting.mAlphaInput.mTevColor1 = (byte)value; SignalPropertyChange(); } }
        [Category("Alpha Input")]
        public EmitterDrawSetting.ColorInput.TevColor amTevColor2 { get { return (EmitterDrawSetting.ColorInput.TevColor)drawSetting.mAlphaInput.mTevColor2; } set { drawSetting.mAlphaInput.mTevColor2 = (byte)value; SignalPropertyChange(); } }
        [Category("Alpha Input")]
        public EmitterDrawSetting.ColorInput.TevColor amTevColor3 { get { return (EmitterDrawSetting.ColorInput.TevColor)drawSetting.mAlphaInput.mTevColor3; } set { drawSetting.mAlphaInput.mTevColor3 = (byte)value; SignalPropertyChange(); } }
        [Category("Alpha Input")]
        public EmitterDrawSetting.ColorInput.TevColor amTevKColor1 { get { return (EmitterDrawSetting.ColorInput.TevColor)drawSetting.mAlphaInput.mTevKColor1; } set { drawSetting.mAlphaInput.mTevKColor1 = (byte)value; SignalPropertyChange(); } }
        [Category("Alpha Input")]
        public EmitterDrawSetting.ColorInput.TevColor amTevKColor2 { get { return (EmitterDrawSetting.ColorInput.TevColor)drawSetting.mAlphaInput.mTevKColor2; } set { drawSetting.mAlphaInput.mTevKColor2 = (byte)value; SignalPropertyChange(); } }
        [Category("Alpha Input")]
        public EmitterDrawSetting.ColorInput.TevColor amTevKColor3 { get { return (EmitterDrawSetting.ColorInput.TevColor)drawSetting.mAlphaInput.mTevKColor3; } set { drawSetting.mAlphaInput.mTevKColor3 = (byte)value; SignalPropertyChange(); } }
        [Category("Alpha Input")]
        public EmitterDrawSetting.ColorInput.TevColor amTevKColor4 { get { return (EmitterDrawSetting.ColorInput.TevColor)drawSetting.mAlphaInput.mTevKColor4; } set { drawSetting.mAlphaInput.mTevKColor4 = (byte)value; SignalPropertyChange(); } }

        public GXCompare mZCompareFunc { get { return (GXCompare)drawSetting.mZCompareFunc; } set { drawSetting.mZCompareFunc = (byte)value; SignalPropertyChange(); } }
        public EmitterDrawSetting.AlphaFlickType mAlphaFlickType { get { return (EmitterDrawSetting.AlphaFlickType)drawSetting.mAlphaFlickType; } set { drawSetting.mAlphaFlickType = (byte)value; SignalPropertyChange(); } }
        public ushort mAlphaFlickCycle { get { return drawSetting.mAlphaFlickCycle; } set { drawSetting.mAlphaFlickCycle = value; SignalPropertyChange(); } }
        public byte mAlphaFlickRandom { get { return drawSetting.mAlphaFlickRandom; } set { drawSetting.mAlphaFlickRandom = value; SignalPropertyChange(); } }
        public byte mAlphaFlickAmplitude { get { return drawSetting.mAlphaFlickAmplitude; } set { drawSetting.mAlphaFlickAmplitude = value; SignalPropertyChange(); } }

        //mLighting 
        [Category("Lighting")]
        public EmitterDrawSetting.Lighting.Mode mMode { get { return (EmitterDrawSetting.Lighting.Mode)drawSetting.mLighting.mMode; } set { drawSetting.mLighting.mMode = (byte)value; SignalPropertyChange(); } }
        [Category("Lighting")]
        public EmitterDrawSetting.Lighting.Type mlType { get { return (EmitterDrawSetting.Lighting.Type)drawSetting.mLighting.mType; } set { drawSetting.mLighting.mMode = (byte)value; SignalPropertyChange(); } }
        [Category("Lighting")]
        public RGBAPixel mAmbient { get { return drawSetting.mLighting.mAmbient; } set { drawSetting.mLighting.mAmbient = value; SignalPropertyChange(); } }
        [Category("Lighting")]
        public RGBAPixel mDiffuse { get { return drawSetting.mLighting.mDiffuse; } set { drawSetting.mLighting.mDiffuse = value; SignalPropertyChange(); } }
        [Category("Lighting")]
        public float mRadius { get { return drawSetting.mLighting.mRadius; } set { drawSetting.mLighting.mRadius = value; SignalPropertyChange(); } }
        [Category("Lighting")]
        public Vector3 mPosition { get { return drawSetting.mLighting.mPosition; } set { drawSetting.mLighting.mPosition = value; SignalPropertyChange(); } }

        //public fixed float mIndTexOffsetMtx[6] { get { return drawSetting.mFlags; } set { drawSetting.mFlags = value; SignalPropertyChange(); } } //2x3 Matrix
        public sbyte mIndTexScaleExp { get { return drawSetting.mIndTexScaleExp; } set { drawSetting.mIndTexScaleExp = value; SignalPropertyChange(); } }
        public sbyte pivotX { get { return drawSetting.pivotX; } set { drawSetting.pivotX = value; SignalPropertyChange(); } }
        public sbyte pivotY { get { return drawSetting.pivotY; } set { drawSetting.pivotY = value; SignalPropertyChange(); } }
        //public byte padding { get { return drawSetting.padding; } set { drawSetting.padding = value; SignalPropertyChange(); } }
        public byte ptcltype { get { return drawSetting.ptcltype; } set { drawSetting.ptcltype = value; SignalPropertyChange(); } }
        public byte typeOption { get { return drawSetting.typeOption; } set { drawSetting.typeOption = value; SignalPropertyChange(); } }
        public byte typeDir { get { return drawSetting.typeDir; } set { drawSetting.typeDir = value; SignalPropertyChange(); } }
        public byte typeAxis { get { return drawSetting.typeAxis; } set { drawSetting.typeAxis = value; SignalPropertyChange(); } }
        public byte typeOption0 { get { return drawSetting.typeOption0; } set { drawSetting.typeOption0 = value; SignalPropertyChange(); } }
        public byte typeOption1 { get { return drawSetting.typeOption1; } set { drawSetting.typeOption1 = value; SignalPropertyChange(); } }
        public byte typeOption2 { get { return drawSetting.typeOption2; } set { drawSetting.typeOption2 = value; SignalPropertyChange(); } }
        //public byte padding4 { get { return drawSetting.padding4; } set { drawSetting.padding4 = value; SignalPropertyChange(); } }
        public float zOffset { get { return drawSetting.zOffset; } set { drawSetting.zOffset = value; SignalPropertyChange(); } }

        #endregion

        EmitterDrawSetting drawSetting;

        protected override bool OnInitialize()
        {
            base.OnInitialize();

            _name = "Emitter";

            desc = *Descriptor;
            drawSetting = desc.drawSetting;
            
            return mNumTevs > 0;
        }

        protected override void OnPopulate()
        {
            int col1 = 4;
            int colop1 = col1 + 16;
            int alpha1 = colop1 + 20;
            int alphaop1 = alpha1 + 16;
            int csel1 = alphaop1 + 20;
            for (int i = 0; i < 4; i++)
            {
                REFFTEVStage s = new REFFTEVStage(i);

                fixed (byte* p = &drawSetting.mTevTexture1)
                {
                    s.kcsel = p[csel1 + i];
                    s.kasel = p[csel1 + 4 + i];

                    s.cseld = p[col1 + 4 * i + 3];
                    s.cselc = p[col1 + 4 * i + 2];
                    s.cselb = p[col1 + 4 * i + 1];
                    s.csela = p[col1 + 4 * i + 0];

                    s.cop = p[colop1 + 5 * i + 0];
                    s.cbias = p[colop1 + 5 * i + 1];
                    s.cshift = p[colop1 + 5 * i + 2];
                    s.cclamp = p[colop1 + 5 * i + 3];
                    s.cdest = p[colop1 + 5 * i + 4];

                    s.aseld = p[alpha1 + 4 * i + 3];
                    s.aselc = p[alpha1 + 4 * i + 2];
                    s.aselb = p[alpha1 + 4 * i + 1];
                    s.asela = p[alpha1 + 4 * i + 0];

                    s.aop = p[alphaop1 + 5 * i + 0];
                    s.abias = p[alphaop1 + 5 * i + 1];
                    s.ashift = p[alphaop1 + 5 * i + 2];
                    s.aclamp = p[alphaop1 + 5 * i + 3];
                    s.adest = p[alphaop1 + 5 * i + 4];
                }

                s.ti = 0; 
                s.tc = 0;
                s.cc = 0;
                s.te = false;

                s.Parent = this;
            }
        }
    }
}