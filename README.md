# ViconCameraCalibrationTool
A tool based on augemeted realty which helps to calibrate a Vicon camera system.

## Verwendete Hard- und Software
- Microsoft HoloLens 2
- Unity 2020.3.24f1 LTS
- Microsoft Visual Studio Community 2019
- .NET Framework 5.0
- Microsoft Mixed Reality Toolkit 2.7.2

## Howto

- Projekt in Unity öffnen
- File->Build Settings
	- Universal Windows Platform
	- Target Device: HoloLens
	- Architecture: ARM64
- Im selben Fenster auf "build"
- Speicherort auswählen und Projekt generieren lassen
- Visual Studio Projekt (.sln Datei) öffnen und projekt bauen
- HoloLens und PC müssen beide im developer Modus sein und "Device Portal" muss aktiviert sein (Windows Settings->Privacy & Security->For developers)
- In Visual Studio auf "Run on remote device" klicken (HoloLens und PC müssen im gleichen Netzwerk sein)
- ggf. Code eingeben, der auf HoloLens angezeigt wird