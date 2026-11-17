using UnityEngine;

namespace AnimeShaders
{
    /** <summary>Every global and material property name of the pack's shaders, and its property id, held once.</summary> */
    public static class AnimeShaderIds
    {
        #region Properties

        /** <summary>The mesh channel (UV3) that carries the baked outline normals, in tangent space.</summary> */
        public const int OutlineNormalChannel = 3;

        /** <summary>The asset label that opts a model into the outline normal bake.</summary> */
        public const string OutlineNormalLabel = "AnimeOutlineNormals";

        /** <summary>The light mode of the line pass (enable or disable it on a material by this name).</summary> */
        public const string OutlineLightMode = "SRPDefaultUnlit";

        /** <summary>The light mode of the depth-edge rim pass, drawn by <c>AnimeRimFeature</c>.</summary> */
        public const string RimLightMode = "AnimeRim";

        /** <summary>The face shader's name.</summary> */
        public const string FaceShader = "Anime/Face";

        /** <summary>The hair shader's name.</summary> */
        public const string HairShader = "Anime/Hair";

        /** <summary>The light mode of the hair's depth pass, drawn by <c>AnimeHairDepthFeature</c>.</summary> */
        public const string HairDepthLightMode = "AnimeHairDepth";

        #endregion

        /** <summary>The head frame's per-renderer values (set through <see cref="AnimeBlocks" /> by <see cref="AnimeHeadFrame" />): world space.</summary> */
        public static class Head
        {
            /** <summary>The name _AnimeHeadCentre.</summary> */
            public const string CentreName = "_AnimeHeadCentre";

            /** <summary>The property id of _AnimeHeadCentre.</summary> */
            public static readonly int Centre = Shader.PropertyToID(CentreName);

            /** <summary>The name _AnimeHeadForward.</summary> */
            public const string ForwardName = "_AnimeHeadForward";

            /** <summary>The property id of _AnimeHeadForward.</summary> */
            public static readonly int Forward = Shader.PropertyToID(ForwardName);

            /** <summary>The name _AnimeHeadRight.</summary> */
            public const string RightName = "_AnimeHeadRight";

            /** <summary>The property id of _AnimeHeadRight.</summary> */
            public static readonly int Right = Shader.PropertyToID(RightName);

            /** <summary>The name _AnimeHeadUp.</summary> */
            public const string UpName = "_AnimeHeadUp";

            /** <summary>The property id of _AnimeHeadUp.</summary> */
            public static readonly int Up = Shader.PropertyToID(UpName);
        }

        /** <summary>The hair's depth, a global texture written by <c>AnimeHairDepthFeature</c> for the face's front-hair shadow.</summary> */
        public static class HairDepth
        {
            /** <summary>The name _AnimeHairDepth.</summary> */
            public const string TextureName = "_AnimeHairDepth";

            /** <summary>The property id of _AnimeHairDepth.</summary> */
            public static readonly int Texture = Shader.PropertyToID(TextureName);

            /** <summary>The name _AnimeHairDepthOn: 1 while the texture holds this camera's hair, 0 (never set included) otherwise.</summary> */
            public const string OnName = "_AnimeHairDepthOn";

            /** <summary>The property id of _AnimeHairDepthOn.</summary> */
            public static readonly int On = Shader.PropertyToID(OnName);

            /** <summary>The name _AnimeHairDepthSize: the texture's width and height in texels (xy), and their reciprocals (zw).</summary> */
            public const string SizeName = "_AnimeHairDepthSize";

            /** <summary>The property id of _AnimeHairDepthSize.</summary> */
            public static readonly int Size = Shader.PropertyToID(SizeName);
        }

        /** <summary>The light director's global properties: all zero when never set, which means no change.</summary> */
        public static class Director
        {
            #region Properties

            /** <summary>The name _AnimeDirectorLightDirection.</summary> */
            public const string LightDirectionName = "_AnimeDirectorLightDirection";

            /** <summary>The property id of _AnimeDirectorLightDirection.</summary> */
            public static readonly int LightDirection = Shader.PropertyToID(LightDirectionName);

            /** <summary>The name _AnimeDirectorLightColor.</summary> */
            public const string LightColorName = "_AnimeDirectorLightColor";

            /** <summary>The property id of _AnimeDirectorLightColor.</summary> */
            public static readonly int LightColor = Shader.PropertyToID(LightColorName);

            /** <summary>The name _AnimeDirectorShadowColor.</summary> */
            public const string ShadowColorName = "_AnimeDirectorShadowColor";

            /** <summary>The property id of _AnimeDirectorShadowColor.</summary> */
            public static readonly int ShadowColor = Shader.PropertyToID(ShadowColorName);

            /** <summary>The name _AnimeDirectorAmbient.</summary> */
            public const string AmbientName = "_AnimeDirectorAmbient";

            /** <summary>The property id of _AnimeDirectorAmbient.</summary> */
            public static readonly int Ambient = Shader.PropertyToID(AmbientName);

            /** <summary>The name _AnimeDirectorScalars.</summary> */
            public const string ScalarsName = "_AnimeDirectorScalars";

            /** <summary>The property id of _AnimeDirectorScalars.</summary> */
            public static readonly int Scalars = Shader.PropertyToID(ScalarsName);

            #endregion
        }

        /** <summary>The sun shafts' shader and the properties <c>AnimeSunShaftsPass</c> sets on its material.</summary> */
        public static class SunShafts
        {
            #region Properties

            /** <summary>The shafts' shader's name.</summary> */
            public const string ShaderName = "Hidden/AnimeShaders/SunShafts";

            /** <summary>The name _SunShaftsSun.</summary> */
            public const string SunName = "_SunShaftsSun";

            /** <summary>The property id of _SunShaftsSun.</summary> */
            public static readonly int Sun = Shader.PropertyToID(SunName);

            /** <summary>The name _SunShaftsBlur.</summary> */
            public const string BlurName = "_SunShaftsBlur";

            /** <summary>The property id of _SunShaftsBlur.</summary> */
            public static readonly int Blur = Shader.PropertyToID(BlurName);

            /** <summary>The name _SunShaftsColor.</summary> */
            public const string ColorName = "_SunShaftsColor";

            /** <summary>The property id of _SunShaftsColor.</summary> */
            public static readonly int Color = Shader.PropertyToID(ColorName);

            #endregion
        }

        /** <summary>Every per-material property and texture the shaders read.</summary> */
        public static class MaterialProperties
        {
            #region Properties

            /** <summary>The name _BaseMap.</summary> */
            public const string BaseMapName = "_BaseMap";

            /** <summary>The property id of _BaseMap.</summary> */
            public static readonly int BaseMap = Shader.PropertyToID(BaseMapName);

            /** <summary>The name _BumpMap.</summary> */
            public const string BumpMapName = "_BumpMap";

            /** <summary>The property id of _BumpMap.</summary> */
            public static readonly int BumpMap = Shader.PropertyToID(BumpMapName);

            /** <summary>The name _EmissionMap.</summary> */
            public const string EmissionMapName = "_EmissionMap";

            /** <summary>The property id of _EmissionMap.</summary> */
            public static readonly int EmissionMap = Shader.PropertyToID(EmissionMapName);

            /** <summary>The name _BaseMap_ST.</summary> */
            public const string BaseMapStName = "_BaseMap_ST";

            /** <summary>The property id of _BaseMap_ST.</summary> */
            public static readonly int BaseMapSt = Shader.PropertyToID(BaseMapStName);

            /** <summary>The name _BaseColor.</summary> */
            public const string BaseColorName = "_BaseColor";

            /** <summary>The property id of _BaseColor.</summary> */
            public static readonly int BaseColor = Shader.PropertyToID(BaseColorName);

            /** <summary>The name _ShadowTint.</summary> */
            public const string ShadowTintName = "_ShadowTint";

            /** <summary>The property id of _ShadowTint.</summary> */
            public static readonly int ShadowTint = Shader.PropertyToID(ShadowTintName);

            /** <summary>The name _Band2Tint.</summary> */
            public const string Band2TintName = "_Band2Tint";

            /** <summary>The property id of _Band2Tint.</summary> */
            public static readonly int Band2Tint = Shader.PropertyToID(Band2TintName);

            /** <summary>The name _InsideTint.</summary> */
            public const string InsideTintName = "_InsideTint";

            /** <summary>The property id of _InsideTint.</summary> */
            public static readonly int InsideTint = Shader.PropertyToID(InsideTintName);

            /** <summary>The name _RimColor.</summary> */
            public const string RimColorName = "_RimColor";

            /** <summary>The property id of _RimColor.</summary> */
            public static readonly int RimColor = Shader.PropertyToID(RimColorName);

            /** <summary>The name _EmissionColor.</summary> */
            public const string EmissionColorName = "_EmissionColor";

            /** <summary>The property id of _EmissionColor.</summary> */
            public static readonly int EmissionColor = Shader.PropertyToID(EmissionColorName);

            /** <summary>The name _OutlineColor.</summary> */
            public const string OutlineColorName = "_OutlineColor";

            /** <summary>The property id of _OutlineColor.</summary> */
            public static readonly int OutlineColor = Shader.PropertyToID(OutlineColorName);

            /** <summary>The name _Cutoff.</summary> */
            public const string CutoffName = "_Cutoff";

            /** <summary>The property id of _Cutoff.</summary> */
            public static readonly int Cutoff = Shader.PropertyToID(CutoffName);

            /** <summary>The name _BumpScale.</summary> */
            public const string BumpScaleName = "_BumpScale";

            /** <summary>The property id of _BumpScale.</summary> */
            public static readonly int BumpScale = Shader.PropertyToID(BumpScaleName);

            /** <summary>The name _ShadowThreshold.</summary> */
            public const string ShadowThresholdName = "_ShadowThreshold";

            /** <summary>The property id of _ShadowThreshold.</summary> */
            public static readonly int ShadowThreshold = Shader.PropertyToID(ShadowThresholdName);

            /** <summary>The name _ShadowSoftness.</summary> */
            public const string ShadowSoftnessName = "_ShadowSoftness";

            /** <summary>The property id of _ShadowSoftness.</summary> */
            public static readonly int ShadowSoftness = Shader.PropertyToID(ShadowSoftnessName);

            /** <summary>The name _Band2Offset.</summary> */
            public const string Band2OffsetName = "_Band2Offset";

            /** <summary>The property id of _Band2Offset.</summary> */
            public static readonly int Band2Offset = Shader.PropertyToID(Band2OffsetName);

            /** <summary>The name _Band2Softness.</summary> */
            public const string Band2SoftnessName = "_Band2Softness";

            /** <summary>The property id of _Band2Softness.</summary> */
            public static readonly int Band2Softness = Shader.PropertyToID(Band2SoftnessName);

            /** <summary>The name _ContactShading.</summary> */
            public const string ContactShadingName = "_ContactShading";

            /** <summary>The property id of _ContactShading.</summary> */
            public static readonly int ContactShading = Shader.PropertyToID(ContactShadingName);

            /** <summary>The name _ContactThreshold.</summary> */
            public const string ContactThresholdName = "_ContactThreshold";

            /** <summary>The property id of _ContactThreshold.</summary> */
            public static readonly int ContactThreshold = Shader.PropertyToID(ContactThresholdName);

            /** <summary>The name _CloudShadow.</summary> */
            public const string CloudShadowName = "_CloudShadow";

            /** <summary>The property id of _CloudShadow.</summary> */
            public static readonly int CloudShadow = Shader.PropertyToID(CloudShadowName);

            /** <summary>The name _CastShadowStrength.</summary> */
            public const string CastShadowStrengthName = "_CastShadowStrength";

            /** <summary>The property id of _CastShadowStrength.</summary> */
            public static readonly int CastShadowStrength = Shader.PropertyToID(CastShadowStrengthName);

            /** <summary>The name _BodyShadowReach.</summary> */
            public const string BodyShadowReachName = "_BodyShadowReach";

            /** <summary>The property id of _BodyShadowReach.</summary> */
            public static readonly int BodyShadowReach = Shader.PropertyToID(BodyShadowReachName);

            /** <summary>The name _LightInfluence.</summary> */
            public const string LightInfluenceName = "_LightInfluence";

            /** <summary>The property id of _LightInfluence.</summary> */
            public static readonly int LightInfluence = Shader.PropertyToID(LightInfluenceName);

            /** <summary>The name _AmbientStrength.</summary> */
            public const string AmbientStrengthName = "_AmbientStrength";

            /** <summary>The property id of _AmbientStrength.</summary> */
            public static readonly int AmbientStrength = Shader.PropertyToID(AmbientStrengthName);

            /** <summary>The name _ExtraLightStrength.</summary> */
            public const string ExtraLightStrengthName = "_ExtraLightStrength";

            /** <summary>The property id of _ExtraLightStrength.</summary> */
            public static readonly int ExtraLightStrength = Shader.PropertyToID(ExtraLightStrengthName);

            /** <summary>The name _ExtraLightCap.</summary> */
            public const string ExtraLightCapName = "_ExtraLightCap";

            /** <summary>The property id of _ExtraLightCap.</summary> */
            public static readonly int ExtraLightCap = Shader.PropertyToID(ExtraLightCapName);

            /** <summary>The name _RimWidth.</summary> */
            public const string RimWidthName = "_RimWidth";

            /** <summary>The property id of _RimWidth.</summary> */
            public static readonly int RimWidth = Shader.PropertyToID(RimWidthName);

            /** <summary>The name _RimSoftness.</summary> */
            public const string RimSoftnessName = "_RimSoftness";

            /** <summary>The property id of _RimSoftness.</summary> */
            public static readonly int RimSoftness = Shader.PropertyToID(RimSoftnessName);

            /** <summary>The name _RimStrength.</summary> */
            public const string RimStrengthName = "_RimStrength";

            /** <summary>The property id of _RimStrength.</summary> */
            public static readonly int RimStrength = Shader.PropertyToID(RimStrengthName);

            /** <summary>The name _RimLightMask.</summary> */
            public const string RimLightMaskName = "_RimLightMask";

            /** <summary>The property id of _RimLightMask.</summary> */
            public static readonly int RimLightMask = Shader.PropertyToID(RimLightMaskName);

            /** <summary>The switch for the depth-edge rim (1 = on); read by the rim pass.</summary> */
            public const string RimDepthName = "_RimDepth";

            /** <summary>The property id of _RimDepth.</summary> */
            public static readonly int RimDepth = Shader.PropertyToID(RimDepthName);

            /** <summary>The name _RimDepthWidth.</summary> */
            public const string RimDepthWidthName = "_RimDepthWidth";

            /** <summary>The property id of _RimDepthWidth.</summary> */
            public static readonly int RimDepthWidth = Shader.PropertyToID(RimDepthWidthName);

            /** <summary>The name _RimDepthThreshold.</summary> */
            public const string RimDepthThresholdName = "_RimDepthThreshold";

            /** <summary>The property id of _RimDepthThreshold.</summary> */
            public static readonly int RimDepthThreshold = Shader.PropertyToID(RimDepthThresholdName);

            /** <summary>The name _FieldDetail.</summary> */
            public const string FieldDetailName = "_FieldDetail";

            /** <summary>The property id of _FieldDetail.</summary> */
            public static readonly int FieldDetail = Shader.PropertyToID(FieldDetailName);

            /** <summary>The name _FieldTile.</summary> */
            public const string FieldTileName = "_FieldTile";

            /** <summary>The property id of _FieldTile.</summary> */
            public static readonly int FieldTile = Shader.PropertyToID(FieldTileName);

            /** <summary>The name _FieldDark.</summary> */
            public const string FieldDarkName = "_FieldDark";

            /** <summary>The property id of _FieldDark.</summary> */
            public static readonly int FieldDark = Shader.PropertyToID(FieldDarkName);

            /** <summary>The name _FieldLight.</summary> */
            public const string FieldLightName = "_FieldLight";

            /** <summary>The property id of _FieldLight.</summary> */
            public static readonly int FieldLight = Shader.PropertyToID(FieldLightName);

            /** <summary>The name _FieldSecondDark.</summary> */
            public const string FieldSecondDarkName = "_FieldSecondDark";

            /** <summary>The property id of _FieldSecondDark.</summary> */
            public static readonly int FieldSecondDark = Shader.PropertyToID(FieldSecondDarkName);

            /** <summary>The name _FieldSecondLight.</summary> */
            public const string FieldSecondLightName = "_FieldSecondLight";

            /** <summary>The property id of _FieldSecondLight.</summary> */
            public static readonly int FieldSecondLight = Shader.PropertyToID(FieldSecondLightName);

            /** <summary>The name _FieldSteps.</summary> */
            public const string FieldStepsName = "_FieldSteps";

            /** <summary>The property id of _FieldSteps.</summary> */
            public static readonly int FieldSteps = Shader.PropertyToID(FieldStepsName);

            /** <summary>The name _FieldNormal.</summary> */
            public const string FieldNormalName = "_FieldNormal";

            /** <summary>The property id of _FieldNormal.</summary> */
            public static readonly int FieldNormal = Shader.PropertyToID(FieldNormalName);

            /** <summary>The name _FieldBreakUp.</summary> */
            public const string FieldBreakUpName = "_FieldBreakUp";

            /** <summary>The property id of _FieldBreakUp.</summary> */
            public static readonly int FieldBreakUp = Shader.PropertyToID(FieldBreakUpName);

            /** <summary>The name _CircleDetail.</summary> */
            public const string CircleDetailName = "_CircleDetail";

            /** <summary>The property id of _CircleDetail.</summary> */
            public static readonly int CircleDetail = Shader.PropertyToID(CircleDetailName);

            /** <summary>The name _CircleTile.</summary> */
            public const string CircleTileName = "_CircleTile";

            /** <summary>The property id of _CircleTile.</summary> */
            public static readonly int CircleTile = Shader.PropertyToID(CircleTileName);

            /** <summary>The name _CircleDark.</summary> */
            public const string CircleDarkName = "_CircleDark";

            /** <summary>The property id of _CircleDark.</summary> */
            public static readonly int CircleDark = Shader.PropertyToID(CircleDarkName);

            /** <summary>The name _CircleLight.</summary> */
            public const string CircleLightName = "_CircleLight";

            /** <summary>The property id of _CircleLight.</summary> */
            public static readonly int CircleLight = Shader.PropertyToID(CircleLightName);

            /** <summary>The name _CircleSecondDark.</summary> */
            public const string CircleSecondDarkName = "_CircleSecondDark";

            /** <summary>The property id of _CircleSecondDark.</summary> */
            public static readonly int CircleSecondDark = Shader.PropertyToID(CircleSecondDarkName);

            /** <summary>The name _CircleSecondLight.</summary> */
            public const string CircleSecondLightName = "_CircleSecondLight";

            /** <summary>The property id of _CircleSecondLight.</summary> */
            public static readonly int CircleSecondLight = Shader.PropertyToID(CircleSecondLightName);

            /** <summary>The name _CircleSteps.</summary> */
            public const string CircleStepsName = "_CircleSteps";

            /** <summary>The property id of _CircleSteps.</summary> */
            public static readonly int CircleSteps = Shader.PropertyToID(CircleStepsName);

            /** <summary>The name _CircleNormal.</summary> */
            public const string CircleNormalName = "_CircleNormal";

            /** <summary>The property id of _CircleNormal.</summary> */
            public static readonly int CircleNormal = Shader.PropertyToID(CircleNormalName);

            /** <summary>The name _CircleBreakUp.</summary> */
            public const string CircleBreakUpName = "_CircleBreakUp";

            /** <summary>The property id of _CircleBreakUp.</summary> */
            public static readonly int CircleBreakUp = Shader.PropertyToID(CircleBreakUpName);

            /** <summary>The name _FieldStones.</summary> */
            public const string FieldStonesName = "_FieldStones";

            /** <summary>The property id of _FieldStones.</summary> */
            public static readonly int FieldStones = Shader.PropertyToID(FieldStonesName);

            /** <summary>The name _FieldStoneShade.</summary> */
            public const string FieldStoneShadeName = "_FieldStoneShade";

            /** <summary>The property id of _FieldStoneShade.</summary> */
            public static readonly int FieldStoneShade = Shader.PropertyToID(FieldStoneShadeName);

            /** <summary>The name _FieldStoneMix.</summary> */
            public const string FieldStoneMixName = "_FieldStoneMix";

            /** <summary>The property id of _FieldStoneMix.</summary> */
            public static readonly int FieldStoneMix = Shader.PropertyToID(FieldStoneMixName);

            /** <summary>The name _CircleStones.</summary> */
            public const string CircleStonesName = "_CircleStones";

            /** <summary>The property id of _CircleStones.</summary> */
            public static readonly int CircleStones = Shader.PropertyToID(CircleStonesName);

            /** <summary>The name _FaceMap.</summary> */
            public const string FaceMapName = "_FaceMap";

            /** <summary>The property id of _FaceMap.</summary> */
            public static readonly int FaceMap = Shader.PropertyToID(FaceMapName);

            /** <summary>The name _FaceMapStrength.</summary> */
            public const string FaceMapStrengthName = "_FaceMapStrength";

            /** <summary>The property id of _FaceMapStrength.</summary> */
            public static readonly int FaceMapStrength = Shader.PropertyToID(FaceMapStrengthName);

            /** <summary>The name _FaceShadowSoftness.</summary> */
            public const string FaceShadowSoftnessName = "_FaceShadowSoftness";

            /** <summary>The property id of _FaceShadowSoftness.</summary> */
            public static readonly int FaceShadowSoftness = Shader.PropertyToID(FaceShadowSoftnessName);

            /** <summary>The name _FaceShadowBias.</summary> */
            public const string FaceShadowBiasName = "_FaceShadowBias";

            /** <summary>The property id of _FaceShadowBias.</summary> */
            public static readonly int FaceShadowBias = Shader.PropertyToID(FaceShadowBiasName);

            /** <summary>The name _FaceLightHeight.</summary> */
            public const string FaceLightHeightName = "_FaceLightHeight";

            /** <summary>The property id of _FaceLightHeight.</summary> */
            public static readonly int FaceLightHeight = Shader.PropertyToID(FaceLightHeightName);

            /** <summary>The name _HairShadowStrength.</summary> */
            public const string HairShadowStrengthName = "_HairShadowStrength";

            /** <summary>The property id of _HairShadowStrength.</summary> */
            public static readonly int HairShadowStrength = Shader.PropertyToID(HairShadowStrengthName);

            /** <summary>The name _HairShadowReach.</summary> */
            public const string HairShadowReachName = "_HairShadowReach";

            /** <summary>The property id of _HairShadowReach.</summary> */
            public static readonly int HairShadowReach = Shader.PropertyToID(HairShadowReachName);

            /** <summary>The name _HairShadowBias.</summary> */
            public const string HairShadowBiasName = "_HairShadowBias";

            /** <summary>The property id of _HairShadowBias.</summary> */
            public static readonly int HairShadowBias = Shader.PropertyToID(HairShadowBiasName);

            /** <summary>The name _HairShadowSoftness.</summary> */
            public const string HairShadowSoftnessName = "_HairShadowSoftness";

            /** <summary>The property id of _HairShadowSoftness.</summary> */
            public static readonly int HairShadowSoftness = Shader.PropertyToID(HairShadowSoftnessName);

            /** <summary>The name _CircleStoneShade.</summary> */
            public const string CircleStoneShadeName = "_CircleStoneShade";

            /** <summary>The property id of _CircleStoneShade.</summary> */
            public static readonly int CircleStoneShade = Shader.PropertyToID(CircleStoneShadeName);

            /** <summary>The name _CircleStoneMix.</summary> */
            public const string CircleStoneMixName = "_CircleStoneMix";

            /** <summary>The property id of _CircleStoneMix.</summary> */
            public static readonly int CircleStoneMix = Shader.PropertyToID(CircleStoneMixName);

            /** <summary>The name _FieldStoneJoint.</summary> */
            public const string FieldStoneJointName = "_FieldStoneJoint";

            /** <summary>The property id of _FieldStoneJoint.</summary> */
            public static readonly int FieldStoneJoint = Shader.PropertyToID(FieldStoneJointName);

            /** <summary>The name _CircleStoneJoint.</summary> */
            public const string CircleStoneJointName = "_CircleStoneJoint";

            /** <summary>The property id of _CircleStoneJoint.</summary> */
            public static readonly int CircleStoneJoint = Shader.PropertyToID(CircleStoneJointName);

            /** <summary>The name _BreakUpScale.</summary> */
            public const string BreakUpScaleName = "_BreakUpScale";

            /** <summary>The property id of _BreakUpScale.</summary> */
            public static readonly int BreakUpScale = Shader.PropertyToID(BreakUpScaleName);

            /** <summary>The name _BreakUpAngle.</summary> */
            public const string BreakUpAngleName = "_BreakUpAngle";

            /** <summary>The property id of _BreakUpAngle.</summary> */
            public static readonly int BreakUpAngle = Shader.PropertyToID(BreakUpAngleName);

            /** <summary>The name _PatchScale.</summary> */
            public const string PatchScaleName = "_PatchScale";

            /** <summary>The property id of _PatchScale.</summary> */
            public static readonly int PatchScale = Shader.PropertyToID(PatchScaleName);

            /** <summary>The name _PatchStrength.</summary> */
            public const string PatchStrengthName = "_PatchStrength";

            /** <summary>The property id of _PatchStrength.</summary> */
            public static readonly int PatchStrength = Shader.PropertyToID(PatchStrengthName);

            /** <summary>The name _EarthColor.</summary> */
            public const string EarthColorName = "_EarthColor";

            /** <summary>The property id of _EarthColor.</summary> */
            public static readonly int EarthColor = Shader.PropertyToID(EarthColorName);

            /** <summary>The name _EarthAmount.</summary> */
            public const string EarthAmountName = "_EarthAmount";

            /** <summary>The property id of _EarthAmount.</summary> */
            public static readonly int EarthAmount = Shader.PropertyToID(EarthAmountName);

            /** <summary>The name _PatchTint.</summary> */
            public const string PatchTintName = "_PatchTint";

            /** <summary>The property id of _PatchTint.</summary> */
            public static readonly int PatchTint = Shader.PropertyToID(PatchTintName);

            /** <summary>The name _PatchTintScale.</summary> */
            public const string PatchTintScaleName = "_PatchTintScale";

            /** <summary>The property id of _PatchTintScale.</summary> */
            public static readonly int PatchTintScale = Shader.PropertyToID(PatchTintScaleName);

            /** <summary>The name _PatchTintCover.</summary> */
            public const string PatchTintCoverName = "_PatchTintCover";

            /** <summary>The property id of _PatchTintCover.</summary> */
            public static readonly int PatchTintCover = Shader.PropertyToID(PatchTintCoverName);

            /** <summary>The name _PatchTintSoftness.</summary> */
            public const string PatchTintSoftnessName = "_PatchTintSoftness";

            /** <summary>The property id of _PatchTintSoftness.</summary> */
            public static readonly int PatchTintSoftness = Shader.PropertyToID(PatchTintSoftnessName);

            /** <summary>The name _GroundCircle.</summary> */
            public const string GroundCircleName = "_GroundCircle";

            /** <summary>The property id of _GroundCircle.</summary> */
            public static readonly int GroundCircle = Shader.PropertyToID(GroundCircleName);

            /** <summary>The name _EdgeSoftness.</summary> */
            public const string EdgeSoftnessName = "_EdgeSoftness";

            /** <summary>The property id of _EdgeSoftness.</summary> */
            public static readonly int EdgeSoftness = Shader.PropertyToID(EdgeSoftnessName);

            /** <summary>The name _DetailFadeStart.</summary> */
            public const string DetailFadeStartName = "_DetailFadeStart";

            /** <summary>The property id of _DetailFadeStart.</summary> */
            public static readonly int DetailFadeStart = Shader.PropertyToID(DetailFadeStartName);

            /** <summary>The name _DetailFadeRange.</summary> */
            public const string DetailFadeRangeName = "_DetailFadeRange";

            /** <summary>The property id of _DetailFadeRange.</summary> */
            public static readonly int DetailFadeRange = Shader.PropertyToID(DetailFadeRangeName);

            /** <summary>The name _InsideDarken.</summary> */
            public const string InsideDarkenName = "_InsideDarken";

            /** <summary>The property id of _InsideDarken.</summary> */
            public static readonly int InsideDarken = Shader.PropertyToID(InsideDarkenName);

            /** <summary>The name _InsideSaturation.</summary> */
            public const string InsideSaturationName = "_InsideSaturation";

            /** <summary>The property id of _InsideSaturation.</summary> */
            public static readonly int InsideSaturation = Shader.PropertyToID(InsideSaturationName);

            /** <summary>The name _InsideFadeAxis.</summary> */
            public const string InsideFadeAxisName = "_InsideFadeAxis";

            /** <summary>The property id of _InsideFadeAxis.</summary> */
            public static readonly int InsideFadeAxis = Shader.PropertyToID(InsideFadeAxisName);

            /** <summary>The name _InsideFadeStart.</summary> */
            public const string InsideFadeStartName = "_InsideFadeStart";

            /** <summary>The property id of _InsideFadeStart.</summary> */
            public static readonly int InsideFadeStart = Shader.PropertyToID(InsideFadeStartName);

            /** <summary>The name _InsideFadeEnd.</summary> */
            public const string InsideFadeEndName = "_InsideFadeEnd";

            /** <summary>The property id of _InsideFadeEnd.</summary> */
            public static readonly int InsideFadeEnd = Shader.PropertyToID(InsideFadeEndName);

            /** <summary>The name _OutlineWidth.</summary> */
            public const string OutlineWidthName = "_OutlineWidth";

            /** <summary>The property id of _OutlineWidth.</summary> */
            public static readonly int OutlineWidth = Shader.PropertyToID(OutlineWidthName);

            /** <summary>The name _OutlineDistanceMin.</summary> */
            public const string OutlineDistanceMinName = "_OutlineDistanceMin";

            /** <summary>The property id of _OutlineDistanceMin.</summary> */
            public static readonly int OutlineDistanceMin = Shader.PropertyToID(OutlineDistanceMinName);

            /** <summary>The name _OutlineDistanceMax.</summary> */
            public const string OutlineDistanceMaxName = "_OutlineDistanceMax";

            /** <summary>The property id of _OutlineDistanceMax.</summary> */
            public static readonly int OutlineDistanceMax = Shader.PropertyToID(OutlineDistanceMaxName);

            /** <summary>The name _OutlineZOffset.</summary> */
            public const string OutlineZOffsetName = "_OutlineZOffset";

            /** <summary>The property id of _OutlineZOffset.</summary> */
            public static readonly int OutlineZOffset = Shader.PropertyToID(OutlineZOffsetName);

            /** <summary>The name _OutlineVertexZ.</summary> */
            public const string OutlineVertexZName = "_OutlineVertexZ";

            /** <summary>The property id of _OutlineVertexZ.</summary> */
            public static readonly int OutlineVertexZ = Shader.PropertyToID(OutlineVertexZName);

            /** <summary>The name _OutlineWidthChannel.</summary> */
            public const string OutlineWidthChannelName = "_OutlineWidthChannel";

            /** <summary>The property id of _OutlineWidthChannel.</summary> */
            public static readonly int OutlineWidthChannel = Shader.PropertyToID(OutlineWidthChannelName);

            /** <summary>The name _OutlineZChannel.</summary> */
            public const string OutlineZChannelName = "_OutlineZChannel";

            /** <summary>The property id of _OutlineZChannel.</summary> */
            public static readonly int OutlineZChannel = Shader.PropertyToID(OutlineZChannelName);

            /** <summary>The name _OutlineBaseTint.</summary> */
            public const string OutlineBaseTintName = "_OutlineBaseTint";

            /** <summary>The property id of _OutlineBaseTint.</summary> */
            public static readonly int OutlineBaseTint = Shader.PropertyToID(OutlineBaseTintName);

            #endregion
        }

        /**
         * <summary>The material properties that only switch a keyword or set a render state: never read in the
         * shaders' code, so never in the per-material block.</summary>
         */
        public static class Switches
        {
            #region Properties

            /** <summary>The toggle for the second shadow band.</summary> */
            public const string Band2Name = "_Band2";

            /** <summary>The property id of _Band2.</summary> */
            public static readonly int Band2 = Shader.PropertyToID(Band2Name);

            /** <summary>The toggle for receiving cast shadows.</summary> */
            public const string ReceiveShadowsName = "_ReceiveShadows";

            /** <summary>The property id of _ReceiveShadows.</summary> */
            public static readonly int ReceiveShadows = Shader.PropertyToID(ReceiveShadowsName);

            /** <summary>The toggle for reading the cast shadow once for the whole body instead of per surface.</summary> */
            public const string BodyShadowName = "_BodyShadow";

            /** <summary>The property id of _BodyShadow.</summary> */
            public static readonly int BodyShadow = Shader.PropertyToID(BodyShadowName);

            /** <summary>The toggle for additional lights.</summary> */
            public const string ExtraLightsName = "_ExtraLights";

            /** <summary>The property id of _ExtraLights.</summary> */
            public static readonly int ExtraLights = Shader.PropertyToID(ExtraLightsName);

            /** <summary>The toggle for the rim.</summary> */
            public const string RimName = "_Rim";

            /** <summary>The property id of _Rim.</summary> */
            public static readonly int Rim = Shader.PropertyToID(RimName);

            /** <summary>The toggle for emission.</summary> */
            public const string EmissionName = "_Emission";

            /** <summary>The property id of _Emission.</summary> */
            public static readonly int Emission = Shader.PropertyToID(EmissionName);

            /** <summary>The toggle for the emission map.</summary> */
            public const string EmissionMapOnName = "_EmissionMapOn";

            /** <summary>The property id of _EmissionMapOn.</summary> */
            public static readonly int EmissionMapOn = Shader.PropertyToID(EmissionMapOnName);

            /** <summary>The toggle for the normal map.</summary> */
            public const string NormalMapOnName = "_NormalMapOn";

            /** <summary>The property id of _NormalMapOn.</summary> */
            public static readonly int NormalMapOn = Shader.PropertyToID(NormalMapOnName);

            /** <summary>The toggle for alpha clipping.</summary> */
            public const string AlphaClipName = "_AlphaClip";

            /** <summary>The property id of _AlphaClip.</summary> */
            public static readonly int AlphaClip = Shader.PropertyToID(AlphaClipName);

            /** <summary>The toggle for the inside tone.</summary> */
            public const string InsideToneName = "_InsideTone";

            /** <summary>The property id of _InsideTone.</summary> */
            public static readonly int InsideTone = Shader.PropertyToID(InsideToneName);

            /** <summary>The toggle for the inside tone's fade.</summary> */
            public const string InsideFadeName = "_InsideFade";

            /** <summary>The property id of _InsideFade.</summary> */
            public static readonly int InsideFade = Shader.PropertyToID(InsideFadeName);

            /** <summary>The toggle for tinting the line by the base map.</summary> */
            public const string OutlineBaseTintOnName = "_OutlineBaseTintOn";

            /** <summary>The property id of _OutlineBaseTintOn.</summary> */
            public static readonly int OutlineBaseTintOn = Shader.PropertyToID(OutlineBaseTintOnName);

            /** <summary>The toggle for the line reading vertex colour.</summary> */
            public const string OutlineVertexColorName = "_OutlineVertexColor";

            /** <summary>The property id of _OutlineVertexColor.</summary> */
            public static readonly int OutlineVertexColor = Shader.PropertyToID(OutlineVertexColorName);

            /** <summary>The toggle for the line reading baked normals from UV3.</summary> */
            public const string OutlineBakedNormalsName = "_OutlineBakedNormals";

            /** <summary>The property id of _OutlineBakedNormals.</summary> */
            public static readonly int OutlineBakedNormals = Shader.PropertyToID(OutlineBakedNormalsName);

            /** <summary>The toggle for the base map in world XZ (tiling per metre).</summary> */
            public const string WorldUVName = "_WorldUV";

            /** <summary>The property id of _WorldUV.</summary> */
            public static readonly int WorldUV = Shader.PropertyToID(WorldUVName);

            /** <summary>The cull mode (Off draws both sides).</summary> */
            public const string CullName = "_Cull";

            /** <summary>The property id of _Cull.</summary> */
            public static readonly int Cull = Shader.PropertyToID(CullName);

            #endregion
        }

        /** <summary>The sky material's properties (Anime/Sky), written by <c>SkyApply</c>.</summary> */
        public static class Sky
        {
            /** <summary>The sky shader's name.</summary> */
            public const string ShaderName = "Anime/Sky";

            #region Properties

            /** <summary>The name _ZenithColor.</summary> */
            public const string ZenithColorName = "_ZenithColor";

            /** <summary>The property id of _ZenithColor.</summary> */
            public static readonly int ZenithColor = Shader.PropertyToID(ZenithColorName);

            /** <summary>The name _HorizonColor.</summary> */
            public const string HorizonColorName = "_HorizonColor";

            /** <summary>The property id of _HorizonColor.</summary> */
            public static readonly int HorizonColor = Shader.PropertyToID(HorizonColorName);

            /** <summary>The name _BelowColor.</summary> */
            public const string BelowColorName = "_BelowColor";

            /** <summary>The property id of _BelowColor.</summary> */
            public static readonly int BelowColor = Shader.PropertyToID(BelowColorName);

            /** <summary>The name _HorizonHeight.</summary> */
            public const string HorizonHeightName = "_HorizonHeight";

            /** <summary>The property id of _HorizonHeight.</summary> */
            public static readonly int HorizonHeight = Shader.PropertyToID(HorizonHeightName);

            /** <summary>The name _SunColor.</summary> */
            public const string SunColorName = "_SunColor";

            /** <summary>The property id of _SunColor.</summary> */
            public static readonly int SunColor = Shader.PropertyToID(SunColorName);

            /** <summary>The name _SunSize.</summary> */
            public const string SunSizeName = "_SunSize";

            /** <summary>The property id of _SunSize.</summary> */
            public static readonly int SunSize = Shader.PropertyToID(SunSizeName);

            /** <summary>The name _SunStrength.</summary> */
            public const string SunStrengthName = "_SunStrength";

            /** <summary>The property id of _SunStrength.</summary> */
            public static readonly int SunStrength = Shader.PropertyToID(SunStrengthName);

            /** <summary>The name _SunHaloSize.</summary> */
            public const string SunHaloSizeName = "_SunHaloSize";

            /** <summary>The property id of _SunHaloSize.</summary> */
            public static readonly int SunHaloSize = Shader.PropertyToID(SunHaloSizeName);

            /** <summary>The name _SunHaloStrength.</summary> */
            public const string SunHaloStrengthName = "_SunHaloStrength";

            /** <summary>The property id of _SunHaloStrength.</summary> */
            public static readonly int SunHaloStrength = Shader.PropertyToID(SunHaloStrengthName);

            /** <summary>The name _CloudLitColor.</summary> */
            public const string CloudLitColorName = "_CloudLitColor";

            /** <summary>The property id of _CloudLitColor.</summary> */
            public static readonly int CloudLitColor = Shader.PropertyToID(CloudLitColorName);

            /** <summary>The name _CloudShadeColor.</summary> */
            public const string CloudShadeColorName = "_CloudShadeColor";

            /** <summary>The property id of _CloudShadeColor.</summary> */
            public static readonly int CloudShadeColor = Shader.PropertyToID(CloudShadeColorName);

            /** <summary>The name _CloudShadeOffset.</summary> */
            public const string CloudShadeOffsetName = "_CloudShadeOffset";

            /** <summary>The property id of _CloudShadeOffset.</summary> */
            public static readonly int CloudShadeOffset = Shader.PropertyToID(CloudShadeOffsetName);

            /** <summary>The name _CloudHorizonFade.</summary> */
            public const string CloudHorizonFadeName = "_CloudHorizonFade";

            /** <summary>The property id of _CloudHorizonFade.</summary> */
            public static readonly int CloudHorizonFade = Shader.PropertyToID(CloudHorizonFadeName);

            /** <summary>The name _RidgeColor0.</summary> */
            public const string RidgeColor0Name = "_RidgeColor0";

            /** <summary>The property id of _RidgeColor0.</summary> */
            public static readonly int RidgeColor0 = Shader.PropertyToID(RidgeColor0Name);

            /** <summary>The name _RidgeColor1.</summary> */
            public const string RidgeColor1Name = "_RidgeColor1";

            /** <summary>The property id of _RidgeColor1.</summary> */
            public static readonly int RidgeColor1 = Shader.PropertyToID(RidgeColor1Name);

            /** <summary>The name _RidgeColor2.</summary> */
            public const string RidgeColor2Name = "_RidgeColor2";

            /** <summary>The property id of _RidgeColor2.</summary> */
            public static readonly int RidgeColor2 = Shader.PropertyToID(RidgeColor2Name);

            /** <summary>The name _RidgeShape0.</summary> */
            public const string RidgeShape0Name = "_RidgeShape0";

            /** <summary>The property id of _RidgeShape0.</summary> */
            public static readonly int RidgeShape0 = Shader.PropertyToID(RidgeShape0Name);

            /** <summary>The name _RidgeShape1.</summary> */
            public const string RidgeShape1Name = "_RidgeShape1";

            /** <summary>The property id of _RidgeShape1.</summary> */
            public static readonly int RidgeShape1 = Shader.PropertyToID(RidgeShape1Name);

            /** <summary>The name _RidgeShape2.</summary> */
            public const string RidgeShape2Name = "_RidgeShape2";

            /** <summary>The property id of _RidgeShape2.</summary> */
            public static readonly int RidgeShape2 = Shader.PropertyToID(RidgeShape2Name);

            /** <summary>The name _RidgeDistance.</summary> */
            public const string RidgeDistanceName = "_RidgeDistance";

            /** <summary>The property id of _RidgeDistance.</summary> */
            public static readonly int RidgeDistance = Shader.PropertyToID(RidgeDistanceName);

            /** <summary>The name _RidgeLitColor.</summary> */
            public const string RidgeLitColorName = "_RidgeLitColor";

            /** <summary>The property id of _RidgeLitColor.</summary> */
            public static readonly int RidgeLitColor = Shader.PropertyToID(RidgeLitColorName);

            /** <summary>The name _RidgeMist.</summary> */
            public const string RidgeMistName = "_RidgeMist";

            /** <summary>The property id of _RidgeMist.</summary> */
            public static readonly int RidgeMist = Shader.PropertyToID(RidgeMistName);

            /** <summary>The name _Stars.</summary> */
            public const string StarsName = "_Stars";

            /** <summary>The property id of _Stars.</summary> */
            public static readonly int Stars = Shader.PropertyToID(StarsName);

            /** <summary>The name _StarColor.</summary> */
            public const string StarColorName = "_StarColor";

            /** <summary>The property id of _StarColor.</summary> */
            public static readonly int StarColor = Shader.PropertyToID(StarColorName);

            /** <summary>The name _StarDensity.</summary> */
            public const string StarDensityName = "_StarDensity";

            /** <summary>The property id of _StarDensity.</summary> */
            public static readonly int StarDensity = Shader.PropertyToID(StarDensityName);

            /** <summary>The name _StarScale.</summary> */
            public const string StarScaleName = "_StarScale";

            /** <summary>The property id of _StarScale.</summary> */
            public static readonly int StarScale = Shader.PropertyToID(StarScaleName);

            /** <summary>The name _StarSize.</summary> */
            public const string StarSizeName = "_StarSize";

            /** <summary>The property id of _StarSize.</summary> */
            public static readonly int StarSize = Shader.PropertyToID(StarSizeName);

            /** <summary>The name _BloomCeiling.</summary> */
            public const string BloomCeilingName = "_BloomCeiling";

            /** <summary>The property id of _BloomCeiling.</summary> */
            public static readonly int BloomCeiling = Shader.PropertyToID(BloomCeilingName);

            /** <summary>The name _AnimeNoiseTex.</summary> */
            public const string NoiseTexName = "_AnimeNoiseTex";

            /** <summary>The property id of _AnimeNoiseTex.</summary> */
            public static readonly int NoiseTex = Shader.PropertyToID(NoiseTexName);

            /** <summary>The name of the stars keyword.</summary> */
            public const string StarsKeyword = "_ANIME_SKY_STARS";

            #endregion
        }

        /**
         * <summary>The world's global properties, published by <c>SkyApply</c> from a sky preset: the cloud field the sky
         * draws and the cloud shadows read, and the haze. All zero when never set: no clouds, no haze.</summary>
         */
        public static class World
        {
            #region Properties

            /** <summary>The name _AnimeCloudShape.</summary> */
            public const string AnimeCloudShapeName = "_AnimeCloudShape";

            /** <summary>The property id of _AnimeCloudShape.</summary> */
            public static readonly int AnimeCloudShape = Shader.PropertyToID(AnimeCloudShapeName);

            /** <summary>The name _AnimeCloudMotion.</summary> */
            public const string AnimeCloudMotionName = "_AnimeCloudMotion";

            /** <summary>The property id of _AnimeCloudMotion.</summary> */
            public static readonly int AnimeCloudMotion = Shader.PropertyToID(AnimeCloudMotionName);

            /** <summary>The name _AnimeCloudShadowClear.</summary> */
            public const string AnimeCloudShadowClearName = "_AnimeCloudShadowClear";

            /** <summary>The property id of _AnimeCloudShadowClear.</summary> */
            public static readonly int AnimeCloudShadowClear = Shader.PropertyToID(AnimeCloudShadowClearName);

            /** <summary>The name _AnimeHazeColor.</summary> */
            public const string AnimeHazeColorName = "_AnimeHazeColor";

            /** <summary>The property id of _AnimeHazeColor.</summary> */
            public static readonly int AnimeHazeColor = Shader.PropertyToID(AnimeHazeColorName);

            /** <summary>The name _AnimeHazeGlow.</summary> */
            public const string AnimeHazeGlowName = "_AnimeHazeGlow";

            /** <summary>The property id of _AnimeHazeGlow.</summary> */
            public static readonly int AnimeHazeGlow = Shader.PropertyToID(AnimeHazeGlowName);

            /** <summary>The name _AnimeHazeShape.</summary> */
            public const string AnimeHazeShapeName = "_AnimeHazeShape";

            /** <summary>The property id of _AnimeHazeShape.</summary> */
            public static readonly int AnimeHazeShape = Shader.PropertyToID(AnimeHazeShapeName);

            /** <summary>The name _AnimeHazeSky.</summary> */
            public const string AnimeHazeSkyName = "_AnimeHazeSky";

            /** <summary>The property id of _AnimeHazeSky.</summary> */
            public static readonly int AnimeHazeSky = Shader.PropertyToID(AnimeHazeSkyName);

            /** <summary>The name _AnimeWorldShade.</summary> */
            public const string AnimeWorldShadeName = "_AnimeWorldShade";

            /** <summary>The property id of _AnimeWorldShade.</summary> */
            public static readonly int AnimeWorldShade = Shader.PropertyToID(AnimeWorldShadeName);

            /** <summary>The name _AnimeCloudNoise.</summary> */
            public const string AnimeCloudNoiseName = "_AnimeCloudNoise";

            /** <summary>The property id of _AnimeCloudNoise.</summary> */
            public static readonly int AnimeCloudNoise = Shader.PropertyToID(AnimeCloudNoiseName);

            /** <summary>The global keyword that turns the haze on in the pack's shaders (off: the scene's plain fog).</summary> */
            public const string AtmosphereKeyword = "_ANIME_ATMOSPHERE";

            /** <summary>The global keyword that turns the cloud shadows on in the pack's lit shaders.</summary> */
            public const string CloudShadowsKeyword = "_ANIME_CLOUD_SHADOWS";

            #endregion
        }

        /** <summary>The material keywords the shaders declare.</summary> */
        public static class Keywords
        {
            #region Properties

            /** <summary>The name _NORMALMAP.</summary> */
            public const string NormalmapName = "_NORMALMAP";

            /** <summary>The name _RECEIVE_SHADOWS_OFF.</summary> */
            public const string ReceiveShadowsOffName = "_RECEIVE_SHADOWS_OFF";

            /** <summary>The name _ALPHATEST_ON.</summary> */
            public const string AlphatestOnName = "_ALPHATEST_ON";

            /** <summary>The name _ANIME_EXTRA_LIGHTS.</summary> */
            public const string ExtraLightsName = "_ANIME_EXTRA_LIGHTS";

            /** <summary>The name _ANIME_BAND2.</summary> */
            public const string Band2Name = "_ANIME_BAND2";

            /** <summary>The name _ANIME_RIM.</summary> */
            public const string RimName = "_ANIME_RIM";

            /** <summary>The name _ANIME_EMISSION.</summary> */
            public const string EmissionName = "_ANIME_EMISSION";

            /** <summary>The name _ANIME_EMISSION_MAP.</summary> */
            public const string EmissionMapName = "_ANIME_EMISSION_MAP";

            /** <summary>The name _ANIME_INSIDE_TONE.</summary> */
            public const string InsideToneName = "_ANIME_INSIDE_TONE";

            /** <summary>The name _ANIME_BODY_SHADOW.</summary> */
            public const string BodyShadowName = "_ANIME_BODY_SHADOW";

            /** <summary>The name _ANIME_WORLD_UV.</summary> */
            public const string WorldUVName = "_ANIME_WORLD_UV";

            /** <summary>The name _ANIME_INSIDE_FADE.</summary> */
            public const string InsideFadeName = "_ANIME_INSIDE_FADE";

            /** <summary>The name _ANIME_OUTLINE_BAKED.</summary> */
            public const string OutlineBakedName = "_ANIME_OUTLINE_BAKED";

            /** <summary>The name _ANIME_OUTLINE_VCOLOR.</summary> */
            public const string OutlineVcolorName = "_ANIME_OUTLINE_VCOLOR";

            /** <summary>The name _ANIME_OUTLINE_BASETINT.</summary> */
            public const string OutlineBasetintName = "_ANIME_OUTLINE_BASETINT";

            #endregion
        }

        /** <summary>The dither fade's encoding in a renderer's user value.</summary> */
        public static class Fade
        {
            #region Properties

            /** <summary>The user value's low bits that hold the fade level; higher bits are reserved.</summary> */
            public const uint LevelMask = 0xFFu;

            /** <summary>The fade level of a solid renderer (what an unset renderer holds).</summary> */
            public const uint Solid = 0u;

            /** <summary>The fade level of a fully faded renderer: nothing draws, line included.</summary> */
            public const uint Full = 255u;

            #endregion
        }
    }
}
