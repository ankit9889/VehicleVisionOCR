namespace VehicleVisionOCR.Scanner.Core.Enums
{
    public enum ScannerBrand
    {
        Unknown,
        Zebra,
        Honeywell,
        Datalogic,
        Cognex,
        SocketMobile,
        Generic,
        Emulator
    }

    public enum ScannerType
    {
        Unknown,
        Handheld,
        Presentation,
        FixedMount,
        MobileComputer
    }

    public enum ConnectionType
    {
        Unknown,
        USB,
        Bluetooth,
        Serial,
        TcpIp
    }

    public enum ScannerState
    {
        Disconnected,
        Connecting,
        Connected,
        Initializing,
        Ready,
        Scanning,
        Error
    }

    public enum TriggerState
    {
        Released,
        Pressed
    }

    public enum ScanMode
    {
        Single,
        Continuous,
        Presentation
    }

    public enum ImageFormat
    {
        Unknown,
        Jpeg,
        Bmp,
        Tiff,
        Png
    }

    public enum BarcodeFormat
    {
        Unknown,
        Code39,
        Code128,
        DataMatrix,
        QrCode,
        Pdf417,
        Upc,
        Ean
    }
}
