﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using BrawlLib.Imaging;

namespace BrawlLib.SSBBTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct REFF
    {
        //Header + string is aligned to 4 bytes

        public const uint Tag = 0x46464552;

        public SSBBCommonHeader _header;
        public uint _tag; //Same as header
        public bint _dataLength; //Size of second REFF block. (file size - 0x18)
        public bint _dataOffset; //Offset from itself. Begins first entry
        public bint _linkPrev; //0
        public bint _linkNext; //0
        public bshort _stringLen;
        public bshort _padding; //0

        private VoidPtr Address { get { fixed (void* p = &this)return p; } }

        public string IdString
        {
            get { return new String((sbyte*)Address + 0x28); }
            set
            {
                int len = value.Length + 1;
                _stringLen = (short)len;

                byte* dPtr = (byte*)Address + 0x28;
                fixed (char* sPtr = value)
                {
                    for (int i = 0; i < len; i++)
                        *dPtr++ = (byte)sPtr[i];
                }

                //Align to 4 bytes
                while ((len++ & 3) != 0)
                    *dPtr++ = 0;

                //Set data offset
                _dataOffset = 0x18 + len - 1;
            }
        }

        public REFTypeObjectTable* Table { get { return (REFTypeObjectTable*)(Address + 0x18 + _dataOffset); } }
    }

    public unsafe struct REFTypeObjectTable
    {
        //Table size is aligned to 4 bytes
        //All entry offsets are relative to this offset

        public bint _length;
        public bshort _entries;
        public bshort _unk1;

        public VoidPtr Address { get { fixed (void* p = &this)return p; } }

        public REFTypeObjectEntry* First { get { return (REFTypeObjectEntry*)(Address + 8); } }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct REFTypeObjectEntry
    {
        public bshort _strLen;
        public string Name
        {
            get { return new String((sbyte*)Address + 2); }
            set
            {
                int len = value.Length + 1;
                _strLen = (short)len;//.Align(4);

                byte* dPtr = (byte*)Address + 2;
                fixed (char* sPtr = value)
                {
                    for (int i = 0; i < len; i++)
                        *dPtr++ = (byte)sPtr[i];
                }

                //Align to 4 bytes
                //while ((len++ & 3) != 0)
                //    *dPtr++ = 0;
            }
        }

        public int DataOffset
        {
            get { return (int)*(buint*)((byte*)Address + 2 + _strLen); }
            set { *(buint*)((byte*)Address + 2 + _strLen) = (uint)value; }
        }

        public int DataLength
        {
            get { return (int)*(buint*)((byte*)Address + 2 + _strLen + 4); }
            set { *(buint*)((byte*)Address + 2 + _strLen + 4) = (uint)value; }
        }

        private VoidPtr Address { get { fixed (void* p = &this)return p; } }

        public REFTypeObjectEntry* Next { get { return (REFTypeObjectEntry*)(Address + 10 + _strLen); } }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct REFFDataHeader
    {
        public buint _unknown; //0
        public buint _headerSize;

        public EmitterDesc _descriptor;

        public ParticleParameterHeader* _params { get { return (ParticleParameterHeader*)(Address + _headerSize + 8); } }

        public bshort* _ptclTrackCount { get { return (bshort*)((VoidPtr)_params + _params->headersize + 4); } }
        public bshort* _ptclInitTrackCount { get { return _ptclTrackCount + 1; } }
        public bshort* _emitTrackCount { get { return (bshort*)((VoidPtr)_ptclTrackCount + 4 + *_ptclTrackCount * 8); } }
        public bshort* _emitInitTrackCount { get { return _emitTrackCount + 1; } }

        public buint* _ptclTrack { get { return (buint*)((VoidPtr)_ptclTrackCount + 4); } }
        public buint* _emitTrack { get { return (buint*)((VoidPtr)_emitTrackCount + 4); } }

        public VoidPtr _postFieldInfo { get { return (VoidPtr)_emitTrackCount + 4 + *_emitTrackCount * 8; } }
        
        private VoidPtr Address { get { fixed (void* p = &this)return p; } }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct EmitterDesc
    {
        [Flags]
        public enum EmitterCommonFlag : uint
        {
            SyncLife = 0x1,
            Invisible = 0x2,
            MaxLife = 0x4,
            InheritPtclScale = 0x20,
            InheritPtclRotate = 0x40,
            InheritChildEScale = 0x80,
            InheritChildERotate = 0x100,
            DisableCalc = 0x200,
            InheritPtclPivot = 0x400,
            InheritChildPivot = 0x800,
            InheritChildPScale = 0x1000,
            InheritChildPRotate = 0x2000,
            RelocateComplete = 0x80000000,
        }

        public enum EmitFormType
        {
            Disc = 0,
            Line = 1,
            Cube = 5,
            Cylinder = 7,
            Sphere = 8,
            Point = 9,
            Torus = 10
        }

        public buint commonFlag; // EmitterCommonFlag
        public buint emitFlag; // EmitFormType - value & 0xFF
        public bushort emitLife;
        public bushort ptclLife;
        public sbyte ptclLifeRandom;
        public sbyte inheritChildPtclTranslate;
        public sbyte emitEmitIntervalRandom;
        public sbyte emitEmitRandom;
        //0x10
        public bfloat emitEmit;
        public bushort emitEmitStart;
        public bushort emitEmitPast;
        public bushort emitEmitInterval;
        public sbyte inheritPtclTranslate;
        public sbyte inheritChildEmitTranslate;
        public bfloat commonParam1;
        //0x20
        public bfloat commonParam2;
        public bfloat commonParam3;
        public bfloat commonParam4;
        public bfloat commonParam5;
        //0x30
        public bfloat commonParam6;
        public bushort emitEmitDiv;
        public sbyte velInitVelocityRandom;
        public sbyte velMomentumRandom;
        public bfloat velPowerRadiationDir;
        public bfloat velPowerYAxis;
        //0x40
        public bfloat velPowerRandomDir;
        public bfloat velPowerNormalDir;
        public bfloat velDiffusionEmitterNormal;
        public bfloat velPowerSpecDir;
        //0x50
        public bfloat velDiffusionSpecDir;
        public BVec3 velSpecDir;
        //0x60
        public BVec3 scale;
        public BVec3 rotate;
        public BVec3 translate;
        //0x84
        public byte lodNear;
        public byte lodFar;
        public byte lodMinEmit;
        public byte lodAlpha;

        public buint randomSeed;

        public fixed byte userdata[8];
        //0x94
        public EmitterDrawSetting drawSetting;

        public VoidPtr Address { get { fixed (void* ptr = &this)return ptr; } }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct EmitterDrawSetting
    {
        public VoidPtr Address { get { fixed (void* ptr = &this)return ptr; } }

        [Flags]
        public enum DrawFlag : ushort
        {
            ZCompEnable = 0x0001, // 0x0001
            ZUpdate = 0x0002, // 0x0002
            ZCompBeforeTex = 0x0004, // 0x0004
            ClippingDisable = 0x0008, // 0x0008
            UseTex1 = 0x0010, // 0x0010
            UseTex2 = 0x0020, // 0x0020
            UseTexInd = 0x0040, // 0x0040
            ProjTex1 = 0x0080, // 0x0080
            ProjTex2 = 0x0100, // 0x0100
            ProjTexInd = 0x0200, // 0x0200
            Invisible = 0x0400, // 0x0400 1: Does not render
            DrawOrder = 0x0800, // 0x0800 0: normal order, 1: reverse order
            FogEnable = 0x1000, // 0x1000
            XYLinkSize = 0x2000, // 0x2000
            XYLinkScale = 0x4000  // 0x4000
        }
        public bushort mFlags;     // DrawFlag

        public byte mACmpComp0;
        public byte mACmpComp1;
        public byte mACmpOp;

        public byte mNumTevs;   // TEV uses stages 1 through 4
        public byte mFlagClamp; // Obsolete

        [Flags]
        public enum IndirectTargetStage
        {
            None = 0,
            Stage0 = 1,
            Stage1 = 2,
            Stage2 = 4,
            Stage3 = 8
        }
        public byte mIndirectTargetStage;
        //0x8
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TevStageColor
        {
            public byte mA;         // GXTevColorArg / GXTevAlphaArg
            public byte mB;         // GXTevColorArg / GXTevAlphaArg
            public byte mC;         // GXTevColorArg / GXTevAlphaArg
            public byte mD;         // GXTevColorArg / GXTevAlphaArg

            public VoidPtr Address { get { fixed (void* p = &this)return p; } }
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TevStageColorOp
        {
            public byte mOp;        // GXTevOp
            public byte mBias;      // GXTevBias
            public byte mScale;     // GXTevScale
            public byte mClamp;     // GXBool
            public byte mOutReg;    // GXTevRegID

            public VoidPtr Address { get { fixed (void* p = &this)return p; } }
        }

        public byte mTevTexture1;
        public byte mTevTexture2;
        public byte mTevTexture3;
        public byte mTevTexture4;
        public TevStageColor mTevColor1;
        public TevStageColor mTevColor2;
        public TevStageColor mTevColor3;
        public TevStageColor mTevColor4;
        public TevStageColorOp mTevColorOp1;
        public TevStageColorOp mTevColorOp2;
        public TevStageColorOp mTevColorOp3;
        public TevStageColorOp mTevColorOp4;
        public TevStageColor mTevAlpha1;
        public TevStageColor mTevAlpha2;
        public TevStageColor mTevAlpha3;
        public TevStageColor mTevAlpha4;
        public TevStageColorOp mTevAlphaOp1;
        public TevStageColorOp mTevAlphaOp2;
        public TevStageColorOp mTevAlphaOp3;
        public TevStageColorOp mTevAlphaOp4;

        // Constant register selector: GXTevKColorSel
        public byte mTevKColorSel1;
        public byte mTevKColorSel2;
        public byte mTevKColorSel3;
        public byte mTevKColorSel4;

        // Constant register selector: GXTevKAlphaSel
        public byte mTevKAlphaSel1;
        public byte mTevKAlphaSel2;
        public byte mTevKAlphaSel3;
        public byte mTevKAlphaSel4;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BlendMode
        {
            public byte mType;                      // GXBlendMode
            public byte mSrcFactor;                 // GXBlendFactor
            public byte mDstFactor;                 // GXBlendFactor
            public byte mOp;                        // GXLogicOp
        }
        public BlendMode mBlendMode;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ColorInput
        {
            public enum RasColor
            {
                Null = 0,      // No request
                Lighting = 1   // Color lit by lighting
            }
            public enum TevColor
            {
                Null = 0,            // No request
                Layer1Primary = 1,   // Layer 1 primary color
                Layer1Secondary = 2, // Layer 1 Secondary Color
                Layer2Primary = 3,   // Layer 2 primary color
                Layer2Secondary = 4, // Layer 2 Secondary Color
                Layer1Multi = 5,     // Layer 1 primary color x secondary color
                Layer2Multi = 6      // Layer 2 primary color x secondary color
            }

            public byte mRasColor; //Rasterize color (only channel 0): RasColor

            //TEV register: TevColor
            public byte mTevColor1;
            public byte mTevColor2;
            public byte mTevColor3;
              
            //Constant register: TevColor
            public byte mTevKColor1;
            public byte mTevKColor2;
            public byte mTevKColor3;
            public byte mTevKColor4;
        }
        public ColorInput mColorInput;
        public ColorInput mAlphaInput;

        //Length below is 0x48

        public byte mZCompareFunc;          // GXCompare

        // Alpha Swing
        public enum AlphaFlickType : byte
        {
            None = 0,
            Triangle,
            SawTooth1,
            SawTooth2,
            Square,
            Sine
        }
        public byte mAlphaFlickType;        // AlphaFlickType

        public bushort mAlphaFlickCycle;
        public byte mAlphaFlickRandom;
        public byte mAlphaFlickAmplitude;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Lighting
        {
            public enum Mode
            {
                Off = 0,
                Simple,
                Hardware
            }
            public enum Type
            {
                None = 0,
                Ambient,
                Point
            }
            public byte mMode;                  // Mode
            public byte mType;                  // Type

            public RGBAPixel mAmbient;
            public RGBAPixel mDiffuse;
            public bfloat mRadius;
            public BVec3 mPosition;
        }
        public Lighting mLighting;

        public fixed float mIndTexOffsetMtx[6]; //2x3 Matrix
        public sbyte mIndTexScaleExp;

        public sbyte pivotX;
        public sbyte pivotY;
        public byte padding;

        // Expression
        //
        // Stored in ptcltype member.
        public enum Type
        {
            Point = 0,
            Line,
            Free,
            Billboard,
            Directional,
            Stripe,
            SmoothStripe
        }

        // Expression assistance -- everything except billboards
        //
        // Stored in typeOption member.
        public enum Assist
        {
            Normal = 0, // Render single Quad to Face surface
            Cross       // Add Quads so they are orthogonal to Normals.
        }

        // Expression assistance -- billboards
        //
        // Stored in typeOption member.
        public enum BillboardAssist
        {
            Normal = 0,     // Normal
            Y,              // Y-axis billboard
            Directional,    // Billboard using the movement direction as its axis
            NormalNoRoll    // Normal (no roll)
        }

        // Expression assistance -- stripes
        public enum StripeAssist
        {
            Normal = 0,          // Normal.
            Cross,               // Add a surface orthogonal to the Normal.
            Billboard,           // Always faces the screen.
            Tube                 // Expression of a tube shape.
        }

        // Movement direction (Y-axis) -- everything except billboard
        //
        // Stored in typeDir member.
        public enum Ahead
        {
            AHEAD_SPEED = 0,                    // Velocity vector direction
            AHEAD_EMITTER_CENTER,               // Relative position from the center of emitter
            AHEAD_EMITTER_DESIGN,               // Emitter specified direction
            AHEAD_PARTICLE,                     // Difference in location from the previous particle
            AHEAD_USER,                         // User specified (unused)
            AHEAD_NO_DESIGN,                    // Unspecified
            AHEAD_PARTICLE_BOTH,                // Difference in position with both neighboring particles
            AHEAD_NO_DESIGN2,                   // Unspecified (initialized as the world Y-axis)
        }

        // Movement direction (Y-axis) -- billboards
        //
        // Stored in typeDir member.
        public enum BillboardAhead
        {
            BILLBOARD_AHEAD_SPEED = 0,              // Velocity vector direction
            BILLBOARD_AHEAD_EMITTER_CENTER,         // Relative position from the center of emitter
            BILLBOARD_AHEAD_EMITTER_DESIGN,         // Emitter specified direction
            BILLBOARD_AHEAD_PARTICLE,               // Difference in location from the previous particle
            BILLBOARD_AHEAD_PARTICLE_BOTH,          // Difference in position with both neighboring particles
        }

        // Rotational axis to take into account when rendering
        //
        // Stored in typeAxis member.
        public enum RotateAxis
        {
            ROTATE_AXIS_X = 0,          // X-axis rotation only
            ROTATE_AXIS_Y,              // Y-axis rotation only
            ROTATE_AXIS_Z,              // Z-axis rotation only
            ROTATE_AXIS_XYZ,            // 3-axis rotation
        }

        // Base surface (polygon render surface)
        //
        // Stored in typeReference.
        public enum Face
        {
            XY = 0,
            XZ,
        }

        // Stripe terminal connections
        //
        // Stored in typeOption2. >> 0 & 7
        public enum StripeConnect
        {
            None = 0,    // Does not connect
            Ring,        // Both ends connected
            Emitter,     // Connect between the newest particle and the emitter
            //Mask = 0x07 // StripeConnect mask
        }

        // Initial value of the reference axis for stripes
        //
        // Stored in typeOption2. >> 3 & 7
        public enum StripeInitialPrevAxis
        {
            XAxis = 1,   // X-axis of the emitter
            YAxis = 0,   // Y-axis of the emitter (assigned to 0 for compatibility)
            ZAxis = 2,   // Z-axis of the emitter
            XYZ = 3,      // Direction in emitter coordinates (1, 1, 1)
            //STRIPE_INITIAL_PREV_AXIS__MASK = 0x07 << 3          // Bitmask
        }

        // Method of applying texture to stripes
        //
        // Stored in typeOption2. >> 6 & 3
        public enum StripeTexmapType
        {
            Stretch = 0,    // Stretch the texture along the stripe's entire length.
            Repeat = 1,     // Repeats the texture for each segment.
            //STRIPE_TEXMAP_TYPE__MASK = 0x03 << 6
        }

        // Directional axis processing
        //
        // Stored in typeOption2.
        [Flags]
        public enum DirectionalPivot
        {
            NoProcessing = 0 << 0,         // No processing
            Billboard = 1 << 0,   // Convert into a billboard, with the movement direction as its axis
            //DIRECTIONAL_PIVOT__MASK = 0x03 << 0
        }

        public byte ptcltype;                   // enum Type

        public byte typeOption;                 // Expression assistance
        // Billboard:
        //   enum BillboardAssist
        // Linear stripe/smooth stripe:
        //   enum StripeAssist
        // Other:
        //   enum Assist

        public byte typeDir;                    // Movement direction
        // Other:
        //   enum Ahead
        // Billboard:
        //   enum BillboardAhead

        public byte typeAxis;                   // enum RotateAxis
        
        public byte typeOption0;                // Various types of parameters corresponding to the particle shapes
        // Directional:
        //   Change vertical (Y) based on speed : 0=off, 1=on
        // Linear stripe/smooth stripe:
        //   Number of vertices in the tube (3+)

        public byte typeOption1;                // Various types of parameters corresponding to the particle shapes
        // Directional:
        //   enum Face
        // Smooth stripe
        //   Number of interpolation divisions (1+)

        public byte typeOption2;                // Various types of parameters corresponding to the particle shapes
        // Linear stripe/smooth stripe:
        //   enum StripeConnect
        //   | enum StripeInitialPrevAxis
        //   | enum StripeTexmapType
        // Directional:
        //   enum DirectionalPivot
        public byte padding4;
        public bfloat zOffset;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ParticleParameterHeader
    {
        public buint headersize;
        public ParticleParameterDesc paramDesc;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct ParticleParameterDesc
    {
        public RGBAPixel mColor11;
        public RGBAPixel mColor12;
        public RGBAPixel mColor21;
        public RGBAPixel mColor22;

        public BVec2 size;
        public BVec2 scale;
        public BVec3 rotate;

        public BVec2 textureScale1;
        public BVec2 textureScale2;
        public BVec2 textureScale3;

        public BVec3 textureRotate;

        public BVec2 textureTranslate1;
        public BVec2 textureTranslate2;
        public BVec2 textureTranslate3;

        //These three are texture data pointers
        public uint mTexture1;    // 0..1: stage0,1, 2: Indirect
        public uint mTexture2;
        public uint mTexture3;

        public bushort textureWrap;
        public byte textureReverse;

        public byte mACmpRef0;
        public byte mACmpRef1;

        public byte rotateOffsetRandomX;
        public byte rotateOffsetRandomY;
        public byte rotateOffsetRandomZ;

        public BVec3 rotateOffset;

        public bushort textureNames; //align to 4 bytes
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TextureData
    {
        public bushort width;
        public bushort height;
        public buint dataSize; // If changed, this will be corrupted when relocated. Cannot be changed or referenced.
        public byte format;
        public byte pltFormat;
        public bushort pltEntries;
        public buint pltSize; // If changed, this will be corrupted when relocated. Cannot be changed or referenced.
        public byte mipmap;
        public byte min_filt;
        public byte mag_filt;
        public byte reserved1;
        public bfloat lod_bias;
    }

    public enum AnimCurveTargetByteFloat //curve flag = 0, 3
    {
        //Updates: ParticleParam
        Color0Primary = 0,
        Alpha0Primary = 3,
        Color0Secondary = 4,
        Alpha0Secondary = 7,
        Color1Primary = 8,
        Alpha1Primary = 11,
        Color1Secondary = 12,
        Alpha1Secondary = 15,
        Size = 16,
        Scale = 24,
        ACMPref0 = 119,
        ACMPref1 = 120,
        Tex1Scale = 44,
        Tex1Rot = 68,
        Tex1Trans = 80,
        Tex2Scale = 52,
        Tex2Rot = 72,
        Tex2Trans = 88,
        TexIndScale = 60,
        TexIndRot = 76,
        TexIndTrans = 96,
    }

    public enum AnimCurveTargetRotateFloat //curve flag = 6, 3 when baking
    {
        //Updates: ParticleParam
        Rotate = 32
    }

    public enum AnimCurveTargetPtclTex //curve flag = 4
    {
        //Updates: ParticleParam
        Tex1 = 104,
        Tex2 = 108,
        TexInd = 112,
    }
    public enum AnimCurveTargetChild //curve flag = 5
    {
        //Updates: child
        Child = 0
    }

    public enum AnimCurveTargetField //curve flag = 7
    {
        //Updates: Field
        Gravity = 0,
        Speed = 1,
        Magnet = 2,
        Newton = 3,
        Vortex = 4,
        Spin = 6,
        Random = 7,
        Tail = 8,
    }

    public enum AnimCurveTargetPostField //curve flag = 2
    {
        //Updates: PostFieldInfo.AnimatableParams
        Size = 0,
        Rotate = 12,
        Translate = 24,
    }
    public enum AnimCurveTargetEmitterFloat //curve flag = 11
    {
        //Updates: EmitterParam
        CommonParam = 44,
        Scale = 124,
        Rotate = 136,
        Translate = 112,
        SpeedOrig = 72,
        SpeedYAxis = 76,
        SpeedRandom = 80,
        SpeedNormal = 84,
        SpeedSpecDir = 92,
        Emission = 8
    }

    public enum AnimCurveTarget
    {
        /* Update target: ParticleParam*/
        // curveFlag = 0(u8) or 3(f32)
        AC_TARGET_COLOR0PRI = 0,
        AC_TARGET_ALPHA0PRI = 3,
        AC_TARGET_COLOR0SEC = 4,
        AC_TARGET_ALPHA0SEC = 7,
        AC_TARGET_COLOR1PRI = 8,
        AC_TARGET_ALPHA1PRI = 11,
        AC_TARGET_COLOR1SEC = 12,
        AC_TARGET_ALPHA1SEC = 15,
        AC_TARGET_SIZE = 16,
        AC_TARGET_SCALE = 24,
        AC_TARGET_ACMPREF0 = 119,
        AC_TARGET_ACMPREF1 = 120,
        AC_TARGET_TEXTURE1SCALE = 44,
        AC_TARGET_TEXTURE1ROTATE = 68,
        AC_TARGET_TEXTURE1TRANSLATE = 80,
        AC_TARGET_TEXTURE2SCALE = 52,
        AC_TARGET_TEXTURE2ROTATE = 72,
        AC_TARGET_TEXTURE2TRANSLATE = 88,
        AC_TARGET_TEXTUREINDSCALE = 60,
        AC_TARGET_TEXTUREINDROTATE = 76,
        AC_TARGET_TEXTUREINDTRANSLATE = 96,

        // curveFlag = 6 (3 when baking)
        AC_TARGET_ROTATE = 32,

        //curveFlag = 4
        AC_TARGET_TEXTURE1 = 104,
        AC_TARGET_TEXTURE2 = 108,
        AC_TARGET_TEXTUREIND = 112,

        /* Update target: child*/
        //curveFlag = 5
        AC_TARGET_CHILD = 0,

        /* Update target: Field*/
        //curveFlag = 7
        AC_TARGET_FIELD_GRAVITY = 0,
        AC_TARGET_FIELD_SPEED = 1,
        AC_TARGET_FIELD_MAGNET = 2,
        AC_TARGET_FIELD_NEWTON = 3,
        AC_TARGET_FIELD_VORTEX = 4,
        AC_TARGET_FIELD_SPIN = 6,
        AC_TARGET_FIELD_RANDOM = 7,
        AC_TARGET_FIELD_TAIL = 8,

        /* Update target: PostFieldInfo::AnimatableParams*/
        //curveFlag = 2
        AC_TARGET_POSTFIELD_SIZE = 0,
        AC_TARGET_POSTFIELD_ROTATE = 12,
        AC_TARGET_POSTFIELD_TRANSLATE = 24,

        /* Update target: EmitterParam*/
        //curveFlag = 11 (all f32)
        AC_TARGET_EMIT_COMMONPARAM = 44,
        AC_TARGET_EMIT_SCALE = 124,
        AC_TARGET_EMIT_ROTATE = 136,
        AC_TARGET_EMIT_TRANSLATE = 112,
        AC_TARGET_EMIT_SPEED_ORIG = 72,
        AC_TARGET_EMIT_SPEED_YAXIS = 76,
        AC_TARGET_EMIT_SPEED_RANDOM = 80,
        AC_TARGET_EMIT_SPEED_NORMAL = 84,
        AC_TARGET_EMIT_SPEED_SPECDIR = 92,
        AC_TARGET_EMIT_EMISSION = 8
    };

    public enum AnimCurveType
    {
        ParticleByte = 0,
        ParticleFloat = 3,
        ParticleRotate = 6,
        ParticleTexture = 4,
        Child = 5,
        Field = 7,
        PostField = 2,
        EmitterFloat = 11
    }
    [Flags]
    public enum AnimCurveHeaderProcessFlagType
    {
        SyncRAnd = 0x04,
        Stop = 0x08,
        EmitterTiming = 0x10,
        InfiniteLoop = 0x20,
        Turn = 0x40,
        Fitting = 0x80
    }
    public struct AnimCurveHeader
    {
        public byte magic;
        public byte kindType;
        public byte curveFlag;
        public byte kindEnable;
        public byte processFlag;
        public byte loopCount;
        public bushort randomSeed;
        public bushort frameLength;
        public bushort padding;
        public buint keyTable;
        public buint rangeTable;
        public buint randomTable;
        public buint nameTable;
        public buint infoTable;
    }
    public struct AnimCurveTableHeader
    {
        public bushort count;
        public bushort pad;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct PostFieldInfo
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct AnimatableParams
        {
            public BVec3 mSize;
            public BVec3 mRotate;
            public BVec3 mTranslate;
        }
        public AnimatableParams mAnimatableParams;

        public enum ControlSpeedType
        {
            None = 0,
            Limit = 1,
            Terminate = 2
        }
        public bfloat mReferenceSpeed;
        public byte mControlSpeedType;

        public enum CollisionShapeType
        {
            Plane = 0,
            Rectangle = 1,
            Circle = 2,
            Cube = 3,
            Sphere = 4,
            Cylinder = 5
        }
        public byte mCollisionShapeType;
        public enum ShapeOption
        {
            XZ = 0x00,
            XY = 0x01,
            YZ = 0x02,
            Whole = 0x40,
            Top = 0x41,
            Bottom = 0x42,
            None = 0x50
        }
        public enum ShapeOptionPlane
        {
            XZ = 0,
            XY = 1,
            YZ = 2
        }
        public enum ShapeOptionSphere
        {
            Whole = 0,
            Top = 1,
            Bottom = 2
        }
        public byte mCollisionShapeOption; // ShapeOptionPlane | ShapeOptionSphere
        public enum CollisionType
        {
            Border = 0, // Border
            Inner = 1, // Inside, +X, +Y, +Z
            Outer = 2 // Outside, -X, -Y, -Z
        }
        public byte mCollisionType;

        [Flags]
        public enum CollisionOption
        {
            EmitterOriented = 0x1, // Emitter center
            Bounce = 0x2, // Reflection
            ControlSpeed = 0x8, // When speed is controlled in some way other than through reflection or wraparound
            CreateChildPtcl = 0x10, // Child creation (particle creation)
            CreateChildEmit = 0x20, // Child creation (emitter creation)
            Delete = 0x40 // Delete
        }
        public bushort mCollisionOption;

        public bushort mStartFrame;

        public BVec3 mSpeedFactor; // (1,1,1) if not controlled

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ChildOption
        {
            EmitterInheritSetting mInheritSetting;
            public bushort mNameIdx;
        }
        public ChildOption mChildOption;

        [Flags]
        public enum WrapOption
        {
            Enable = 1, // If 0, the Wrap feature is not used
            
            CenterOrigin = 0 << 1, // Center of the global origin
            CenterEmitter = 1 << 1 // Emitter center
        }
        public byte mWrapOption; // Bitwise OR of enum WrapOption

        public fixed byte padding[3];

        public BVec3 mWrapScale;
        public BVec3 mWrapRotate;
        public BVec3 mWrapTranslate;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct EmitterInheritSetting
    {
        public enum EmitterInheritSettingFlag
        {
            FollowEmitter = 1,
            InheritRotate = 2
        }

        public bshort speed;
        public byte scale;
        public byte alpha;
        public byte color;
        public byte weight;
        public byte type;
        public byte flag;
        public byte alphaFuncPri;
        public byte alphaFuncSec;
    }
}