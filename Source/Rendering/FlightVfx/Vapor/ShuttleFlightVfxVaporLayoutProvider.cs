using UnityEngine;

namespace CeleTech.ShuttleExtension.ModularShuttle.Rendering.FlightVfx.Vapor
{
    internal static class ShuttleFlightVfxVaporLayoutProvider
    {
        private const float UpperSideEdgeRibbonRootUvX = 0.48f;
        private const float UpperSideEdgeRibbonRootUvY = 0.72f;
        private const float UpperSideEdgeRibbonTailUvX = 0.32f;
        private const float UpperSideEdgeRibbonTailUvY = 0.72f;
        private const float UpperSideEdgeRibbonOutwardUvX = 0.48f;
        private const float UpperSideEdgeRibbonOutwardUvY = 0.84f;
        private const float UpperSideEdgeRibbonCenterOffsetLocal = 0.12f;
        private const float UpperSideEdgeRibbonLengthAxisDeg = -90f;
        private const float UpperSideEdgeRibbonOutwardNormalDeg = 0f;
        private const float UpperSideEdgeRibbonBaseSizeX = 1.72f;
        private const float UpperSideEdgeRibbonBaseSizeY = 0.20f;
        private const float UpperSideEdgeRibbonSizeMultiplier = 1.00f;
        private const float UpperSideEdgeRibbonLengthMultiplier = 1.00f;
        private const float UpperSideEdgeRibbonWidthMultiplier = 1.00f;
        private const float UpperSideEdgeRibbonStreamwiseOffsetLocal = 0.00f;
        private const float UpperSideEdgeRibbonStrength = 0.14f;

        private const float LowerSideEdgeRibbonRootUvX = 0.48f;
        private const float LowerSideEdgeRibbonRootUvY = 0.34f;
        private const float LowerSideEdgeRibbonTailUvX = 0.32f;
        private const float LowerSideEdgeRibbonTailUvY = 0.34f;
        private const float LowerSideEdgeRibbonOutwardUvX = 0.48f;
        private const float LowerSideEdgeRibbonOutwardUvY = 0.22f;
        private const float LowerSideEdgeRibbonCenterOffsetLocal = 0.12f;
        private const float LowerSideEdgeRibbonLengthAxisDeg = -90f;
        private const float LowerSideEdgeRibbonOutwardNormalDeg = 180f;
        private const float LowerSideEdgeRibbonBaseSizeX = 1.72f;
        private const float LowerSideEdgeRibbonBaseSizeY = 0.20f;
        private const float LowerSideEdgeRibbonSizeMultiplier = 1.00f;
        private const float LowerSideEdgeRibbonLengthMultiplier = 1.00f;
        private const float LowerSideEdgeRibbonWidthMultiplier = 1.00f;
        private const float LowerSideEdgeRibbonStreamwiseOffsetLocal = 0.00f;
        private const float LowerSideEdgeRibbonStrength = 0.14f;

        private const float UpperRearPodOuterShoulderRootUvX = 0.255f;
        private const float UpperRearPodOuterShoulderRootUvY = 0.78f;
        private const float UpperRearPodOuterShoulderTailUvX = 0.145f;
        private const float UpperRearPodOuterShoulderTailUvY = 0.792f;
        private const float UpperRearPodOuterShoulderOutwardUvX = 0.226f;
        private const float UpperRearPodOuterShoulderOutwardUvY = 0.861f;
        private const float UpperRearPodOuterShoulderCenterOffsetLocal = 0.18f;
        private const float UpperRearPodOuterShoulderLengthAxisDeg = -82f;
        private const float UpperRearPodOuterShoulderOutwardNormalDeg = -15f;
        private const float UpperRearPodOuterShoulderBaseSizeX = 2.12f;
        private const float UpperRearPodOuterShoulderBaseSizeY = 0.78f;
        private const float UpperRearPodOuterShoulderSizeMultiplier = 1.00f;
        private const float UpperRearPodOuterShoulderLengthMultiplier = 3.25f;
        private const float UpperRearPodOuterShoulderWidthMultiplier = 0.82f;
        private const float UpperRearPodOuterShoulderStreamwiseOffsetLocal = 0.86f;
        private const float UpperRearPodOuterShoulderStrength = 1.10f;

        private const float LowerRearPodOuterShoulderRootUvX = 0.270f;
        private const float LowerRearPodOuterShoulderRootUvY = 0.315f;
        private const float LowerRearPodOuterShoulderTailUvX = 0.160f;
        private const float LowerRearPodOuterShoulderTailUvY = 0.303f;
        private const float LowerRearPodOuterShoulderOutwardUvX = 0.241f;
        private const float LowerRearPodOuterShoulderOutwardUvY = 0.235f;
        private const float LowerRearPodOuterShoulderCenterOffsetLocal = 0.14f;
        private const float LowerRearPodOuterShoulderLengthAxisDeg = -98f;
        private const float LowerRearPodOuterShoulderOutwardNormalDeg = -165f;
        private const float LowerRearPodOuterShoulderBaseSizeX = 2.06f;
        private const float LowerRearPodOuterShoulderBaseSizeY = 0.76f;
        private const float LowerRearPodOuterShoulderSizeMultiplier = 1.00f;
        private const float LowerRearPodOuterShoulderLengthMultiplier = 3.25f;
        private const float LowerRearPodOuterShoulderWidthMultiplier = 0.82f;
        private const float LowerRearPodOuterShoulderStreamwiseOffsetLocal = 0.86f;
        private const float LowerRearPodOuterShoulderStrength = 1.10f;

        private const float UpperPodRootCurlRootUvX = 0.310f;
        private const float UpperPodRootCurlRootUvY = 0.68f;
        private const float UpperPodRootCurlTailUvX = 0.206f;
        private const float UpperPodRootCurlTailUvY = 0.709f;
        private const float UpperPodRootCurlOutwardUvX = 0.263f;
        private const float UpperPodRootCurlOutwardUvY = 0.756f;
        private const float UpperPodRootCurlCenterOffsetLocal = 0.16f;
        private const float UpperPodRootCurlLengthAxisDeg = -70f;
        private const float UpperPodRootCurlOutwardNormalDeg = -25f;
        private const float UpperPodRootCurlBaseSizeX = 1.42f;
        private const float UpperPodRootCurlBaseSizeY = 0.48f;
        private const float UpperPodRootCurlSizeMultiplier = 1.00f;
        private const float UpperPodRootCurlLengthMultiplier = 1.38f;
        private const float UpperPodRootCurlWidthMultiplier = 1.00f;
        private const float UpperPodRootCurlStreamwiseOffsetLocal = 0.08f;
        private const float UpperPodRootCurlStrength = 0.82f;

        private const float LowerPodRootCurlRootUvX = 0.295f;
        private const float LowerPodRootCurlRootUvY = 0.39f;
        private const float LowerPodRootCurlTailUvX = 0.191f;
        private const float LowerPodRootCurlTailUvY = 0.362f;
        private const float LowerPodRootCurlOutwardUvX = 0.248f;
        private const float LowerPodRootCurlOutwardUvY = 0.315f;
        private const float LowerPodRootCurlCenterOffsetLocal = 0.14f;
        private const float LowerPodRootCurlLengthAxisDeg = -110f;
        private const float LowerPodRootCurlOutwardNormalDeg = -155f;
        private const float LowerPodRootCurlBaseSizeX = 1.42f;
        private const float LowerPodRootCurlBaseSizeY = 0.48f;
        private const float LowerPodRootCurlSizeMultiplier = 1.00f;
        private const float LowerPodRootCurlLengthMultiplier = 1.38f;
        private const float LowerPodRootCurlWidthMultiplier = 1.00f;
        private const float LowerPodRootCurlStreamwiseOffsetLocal = 0.08f;
        private const float LowerPodRootCurlStrength = 0.82f;

        internal static readonly ShuttleFlightVfxVaporAnchor[] Anchors =
            new ShuttleFlightVfxVaporAnchor[]
            {
                new ShuttleFlightVfxVaporAnchor(
                    "UpperSideEdgeRibbon",
                    ShuttleFlightVfxVaporKind.EdgeRibbon,
                    new Vector2(UpperSideEdgeRibbonRootUvX, UpperSideEdgeRibbonRootUvY),
                    new Vector2(UpperSideEdgeRibbonTailUvX, UpperSideEdgeRibbonTailUvY),
                    new Vector2(UpperSideEdgeRibbonOutwardUvX, UpperSideEdgeRibbonOutwardUvY),
                    UpperSideEdgeRibbonCenterOffsetLocal,
                    UpperSideEdgeRibbonLengthAxisDeg,
                    UpperSideEdgeRibbonOutwardNormalDeg,
                    new Vector2(UpperSideEdgeRibbonBaseSizeX, UpperSideEdgeRibbonBaseSizeY),
                    UpperSideEdgeRibbonSizeMultiplier,
                    UpperSideEdgeRibbonLengthMultiplier,
                    UpperSideEdgeRibbonWidthMultiplier,
                    UpperSideEdgeRibbonStreamwiseOffsetLocal,
                    UpperSideEdgeRibbonStrength,
                    5.4f,
                    true,
                    2),
                new ShuttleFlightVfxVaporAnchor(
                    "LowerSideEdgeRibbon",
                    ShuttleFlightVfxVaporKind.EdgeRibbon,
                    new Vector2(LowerSideEdgeRibbonRootUvX, LowerSideEdgeRibbonRootUvY),
                    new Vector2(LowerSideEdgeRibbonTailUvX, LowerSideEdgeRibbonTailUvY),
                    new Vector2(LowerSideEdgeRibbonOutwardUvX, LowerSideEdgeRibbonOutwardUvY),
                    LowerSideEdgeRibbonCenterOffsetLocal,
                    LowerSideEdgeRibbonLengthAxisDeg,
                    LowerSideEdgeRibbonOutwardNormalDeg,
                    new Vector2(LowerSideEdgeRibbonBaseSizeX, LowerSideEdgeRibbonBaseSizeY),
                    LowerSideEdgeRibbonSizeMultiplier,
                    LowerSideEdgeRibbonLengthMultiplier,
                    LowerSideEdgeRibbonWidthMultiplier,
                    LowerSideEdgeRibbonStreamwiseOffsetLocal,
                    LowerSideEdgeRibbonStrength,
                    6.8f,
                    true,
                    2),
                new ShuttleFlightVfxVaporAnchor(
                    "UpperRearPodOuterShoulder",
                    ShuttleFlightVfxVaporKind.ShoulderPuff,
                    new Vector2(UpperRearPodOuterShoulderRootUvX, UpperRearPodOuterShoulderRootUvY),
                    new Vector2(UpperRearPodOuterShoulderTailUvX, UpperRearPodOuterShoulderTailUvY),
                    new Vector2(UpperRearPodOuterShoulderOutwardUvX, UpperRearPodOuterShoulderOutwardUvY),
                    UpperRearPodOuterShoulderCenterOffsetLocal,
                    UpperRearPodOuterShoulderLengthAxisDeg,
                    UpperRearPodOuterShoulderOutwardNormalDeg,
                    new Vector2(UpperRearPodOuterShoulderBaseSizeX, UpperRearPodOuterShoulderBaseSizeY),
                    UpperRearPodOuterShoulderSizeMultiplier,
                    UpperRearPodOuterShoulderLengthMultiplier,
                    UpperRearPodOuterShoulderWidthMultiplier,
                    UpperRearPodOuterShoulderStreamwiseOffsetLocal,
                    UpperRearPodOuterShoulderStrength,
                    0.2f,
                    true,
                    0),
                new ShuttleFlightVfxVaporAnchor(
                    "LowerRearPodOuterShoulder",
                    ShuttleFlightVfxVaporKind.ShoulderPuff,
                    new Vector2(LowerRearPodOuterShoulderRootUvX, LowerRearPodOuterShoulderRootUvY),
                    new Vector2(LowerRearPodOuterShoulderTailUvX, LowerRearPodOuterShoulderTailUvY),
                    new Vector2(LowerRearPodOuterShoulderOutwardUvX, LowerRearPodOuterShoulderOutwardUvY),
                    LowerRearPodOuterShoulderCenterOffsetLocal,
                    LowerRearPodOuterShoulderLengthAxisDeg,
                    LowerRearPodOuterShoulderOutwardNormalDeg,
                    new Vector2(LowerRearPodOuterShoulderBaseSizeX, LowerRearPodOuterShoulderBaseSizeY),
                    LowerRearPodOuterShoulderSizeMultiplier,
                    LowerRearPodOuterShoulderLengthMultiplier,
                    LowerRearPodOuterShoulderWidthMultiplier,
                    LowerRearPodOuterShoulderStreamwiseOffsetLocal,
                    LowerRearPodOuterShoulderStrength,
                    1.6f,
                    true,
                    0),
                new ShuttleFlightVfxVaporAnchor(
                    "UpperPodRootCurl",
                    ShuttleFlightVfxVaporKind.CurlPuff,
                    new Vector2(UpperPodRootCurlRootUvX, UpperPodRootCurlRootUvY),
                    new Vector2(UpperPodRootCurlTailUvX, UpperPodRootCurlTailUvY),
                    new Vector2(UpperPodRootCurlOutwardUvX, UpperPodRootCurlOutwardUvY),
                    UpperPodRootCurlCenterOffsetLocal,
                    UpperPodRootCurlLengthAxisDeg,
                    UpperPodRootCurlOutwardNormalDeg,
                    new Vector2(UpperPodRootCurlBaseSizeX, UpperPodRootCurlBaseSizeY),
                    UpperPodRootCurlSizeMultiplier,
                    UpperPodRootCurlLengthMultiplier,
                    UpperPodRootCurlWidthMultiplier,
                    UpperPodRootCurlStreamwiseOffsetLocal,
                    UpperPodRootCurlStrength,
                    2.9f,
                    true,
                    1),
                new ShuttleFlightVfxVaporAnchor(
                    "LowerPodRootCurl",
                    ShuttleFlightVfxVaporKind.CurlPuff,
                    new Vector2(LowerPodRootCurlRootUvX, LowerPodRootCurlRootUvY),
                    new Vector2(LowerPodRootCurlTailUvX, LowerPodRootCurlTailUvY),
                    new Vector2(LowerPodRootCurlOutwardUvX, LowerPodRootCurlOutwardUvY),
                    LowerPodRootCurlCenterOffsetLocal,
                    LowerPodRootCurlLengthAxisDeg,
                    LowerPodRootCurlOutwardNormalDeg,
                    new Vector2(LowerPodRootCurlBaseSizeX, LowerPodRootCurlBaseSizeY),
                    LowerPodRootCurlSizeMultiplier,
                    LowerPodRootCurlLengthMultiplier,
                    LowerPodRootCurlWidthMultiplier,
                    LowerPodRootCurlStreamwiseOffsetLocal,
                    LowerPodRootCurlStrength,
                    4.1f,
                    true,
                    1),
                new ShuttleFlightVfxVaporAnchor(
                    "UpperTailNozzleOuterWake",
                    ShuttleFlightVfxVaporKind.CurlPuff,
                    new Vector2(0.08f, 0.74f),
                    0.08f,
                    -92f,
                    -20f,
                    new Vector2(0.80f, 0.30f),
                    1.00f,
                    1.00f,
                    1.00f,
                    0f,
                    0.10f,
                    10.4f,
                    true,
                    3),
                new ShuttleFlightVfxVaporAnchor(
                    "LowerTailNozzleOuterWake",
                    ShuttleFlightVfxVaporKind.CurlPuff,
                    new Vector2(0.08f, 0.33f),
                    0.08f,
                    -88f,
                    -160f,
                    new Vector2(0.80f, 0.30f),
                    1.00f,
                    1.00f,
                    1.00f,
                    0f,
                    0.10f,
                    11.8f,
                    true,
                    3)
            };

        internal static bool IsCalibrationAnchor(ShuttleFlightVfxVaporAnchor anchor)
        {
            return anchor.Id == "UpperRearPodOuterShoulder" ||
                anchor.Id == "LowerRearPodOuterShoulder" ||
                anchor.Id == "UpperPodRootCurl" ||
                anchor.Id == "LowerPodRootCurl";
        }

        internal static bool IsRibbonDrawAnchor(ShuttleFlightVfxVaporAnchor anchor)
        {
            return anchor.Id == "UpperRearPodOuterShoulder" ||
                anchor.Id == "LowerRearPodOuterShoulder" ||
                anchor.Id == "UpperPodRootCurl" ||
                anchor.Id == "LowerPodRootCurl";
        }

        internal static bool IsFleckEmitterAnchor(ShuttleFlightVfxVaporAnchor anchor)
        {
            return anchor.Id == "UpperRearPodOuterShoulder" ||
                anchor.Id == "LowerRearPodOuterShoulder" ||
                anchor.Id == "UpperPodRootCurl" ||
                anchor.Id == "LowerPodRootCurl";
        }

        internal static bool IsCoreCalibrationAnchor(ShuttleFlightVfxVaporAnchor anchor)
        {
            return IsCalibrationAnchor(anchor);
        }

        internal static bool IsCoreAnchor(ShuttleFlightVfxVaporAnchor anchor)
        {
            return IsRibbonDrawAnchor(anchor);
        }
    }
}
