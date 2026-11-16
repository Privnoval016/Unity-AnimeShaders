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

        #endregion

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

            /** <summary>The name _GrassColorA.</summary> */
            public const string GrassColorAName = "_GrassColorA";

            /** <summary>The property id of _GrassColorA.</summary> */
            public static readonly int GrassColorA = Shader.PropertyToID(GrassColorAName);

            /** <summary>The name _GrassColorB.</summary> */
            public const string GrassColorBName = "_GrassColorB";

            /** <summary>The property id of _GrassColorB.</summary> */
            public static readonly int GrassColorB = Shader.PropertyToID(GrassColorBName);

            /** <summary>The name _EarthColor.</summary> */
            public const string EarthColorName = "_EarthColor";

            /** <summary>The property id of _EarthColor.</summary> */
            public static readonly int EarthColor = Shader.PropertyToID(EarthColorName);

            /** <summary>The name _StoneColorA.</summary> */
            public const string StoneColorAName = "_StoneColorA";

            /** <summary>The property id of _StoneColorA.</summary> */
            public static readonly int StoneColorA = Shader.PropertyToID(StoneColorAName);

            /** <summary>The name _StoneColorB.</summary> */
            public const string StoneColorBName = "_StoneColorB";

            /** <summary>The property id of _StoneColorB.</summary> */
            public static readonly int StoneColorB = Shader.PropertyToID(StoneColorBName);

            /** <summary>The name _GroutColor.</summary> */
            public const string GroutColorName = "_GroutColor";

            /** <summary>The property id of _GroutColor.</summary> */
            public static readonly int GroutColor = Shader.PropertyToID(GroutColorName);

            /** <summary>The name _GroundCircle.</summary> */
            public const string GroundCircleName = "_GroundCircle";

            /** <summary>The property id of _GroundCircle.</summary> */
            public static readonly int GroundCircle = Shader.PropertyToID(GroundCircleName);

            /** <summary>The name _GrassPatchScale.</summary> */
            public const string GrassPatchScaleName = "_GrassPatchScale";

            /** <summary>The property id of _GrassPatchScale.</summary> */
            public static readonly int GrassPatchScale = Shader.PropertyToID(GrassPatchScaleName);

            /** <summary>The name _GrainScale.</summary> */
            public const string GrainScaleName = "_GrainScale";

            /** <summary>The property id of _GrainScale.</summary> */
            public static readonly int GrainScale = Shader.PropertyToID(GrainScaleName);

            /** <summary>The name _GrainStrength.</summary> */
            public const string GrainStrengthName = "_GrainStrength";

            /** <summary>The property id of _GrainStrength.</summary> */
            public static readonly int GrainStrength = Shader.PropertyToID(GrainStrengthName);

            /** <summary>The name _EarthAmount.</summary> */
            public const string EarthAmountName = "_EarthAmount";

            /** <summary>The property id of _EarthAmount.</summary> */
            public static readonly int EarthAmount = Shader.PropertyToID(EarthAmountName);

            /** <summary>The name _StoneSize.</summary> */
            public const string StoneSizeName = "_StoneSize";

            /** <summary>The property id of _StoneSize.</summary> */
            public static readonly int StoneSize = Shader.PropertyToID(StoneSizeName);

            /** <summary>The name _GroutWidth.</summary> */
            public const string GroutWidthName = "_GroutWidth";

            /** <summary>The property id of _GroutWidth.</summary> */
            public static readonly int GroutWidth = Shader.PropertyToID(GroutWidthName);

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

        /** <summary>Anime/Sky's property names and ids, and its stars keyword.</summary> */
        public static class Sky
        {
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

            /** <summary>The name _CloudCover.</summary> */
            public const string CloudCoverName = "_CloudCover";

            /** <summary>The property id of _CloudCover.</summary> */
            public static readonly int CloudCover = Shader.PropertyToID(CloudCoverName);

            /** <summary>The name _CloudSoftness.</summary> */
            public const string CloudSoftnessName = "_CloudSoftness";

            /** <summary>The property id of _CloudSoftness.</summary> */
            public static readonly int CloudSoftness = Shader.PropertyToID(CloudSoftnessName);

            /** <summary>The name _CloudScale.</summary> */
            public const string CloudScaleName = "_CloudScale";

            /** <summary>The property id of _CloudScale.</summary> */
            public static readonly int CloudScale = Shader.PropertyToID(CloudScaleName);

            /** <summary>The name _CloudSpeed.</summary> */
            public const string CloudSpeedName = "_CloudSpeed";

            /** <summary>The property id of _CloudSpeed.</summary> */
            public static readonly int CloudSpeed = Shader.PropertyToID(CloudSpeedName);

            /** <summary>The name _CloudWind.</summary> */
            public const string CloudWindName = "_CloudWind";

            /** <summary>The property id of _CloudWind.</summary> */
            public static readonly int CloudWind = Shader.PropertyToID(CloudWindName);

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

            /** <summary>The name _BloomCeiling.</summary> */
            public const string BloomCeilingName = "_BloomCeiling";

            /** <summary>The property id of _BloomCeiling.</summary> */
            public static readonly int BloomCeiling = Shader.PropertyToID(BloomCeilingName);

            /** <summary>The name _Stars.</summary> */
            public const string StarsName = "_Stars";

            /** <summary>The property id of _Stars.</summary> */
            public static readonly int Stars = Shader.PropertyToID(StarsName);

            /** <summary>The name _AnimeNoiseTex.</summary> */
            public const string NoiseTexName = "_AnimeNoiseTex";

            /** <summary>The property id of _AnimeNoiseTex.</summary> */
            public static readonly int NoiseTex = Shader.PropertyToID(NoiseTexName);

            /** <summary>The name of the stars keyword.</summary> */
            public const string StarsKeyword = "_ANIME_SKY_STARS";

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
