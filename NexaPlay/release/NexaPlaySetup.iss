#define MyAppName "NexaPlay"
#define MyAppVersion "1.0.3"
#define MyAppPublisher "NexaPlay"
#define MyAppExeName "NexaPlay.exe"
#define MyAppSourceDir "..\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"
#define MyAppIconFile "..\Assets\Icons\app.ico"

#define MyWizardImageFile "..\Assets\Installer\anim\frame_0001.bmp"
#define MyWizardSmallImageFile "..\Assets\Installer\wizard-small.bmp"
#define MyWizardAnimDir "..\Assets\Installer\anim"

#define MyOutputDir "output"
#define MyOutputBaseFilename "NexaPlay-Setup"

[Setup]
AppId={{A8F6D6C1-9A0D-4D40-8A9B-2DCA4D3C7C01}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}

DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

Compression=lzma
SolidCompression=yes

WizardStyle=modern

; Gambar besar untuk halaman Welcome dan Finish
WizardImageFile={#MyWizardImageFile}
WizardImageStretch=no
WizardImageBackColor=$000000

; WizardSmallImageFile bawaan Inno sengaja tidak dipakai
; karena bisa memunculkan area/background putih.
; Logo kecil akan ditempel manual lewat [Code].
; WizardSmallImageFile={#MyWizardSmallImageFile}

OutputDir={#MyOutputDir}
OutputBaseFilename={#MyOutputBaseFilename}
SetupIconFile={#MyAppIconFile}

SetupLogging=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

CloseApplications=yes
CloseApplicationsFilter={#MyAppExeName}
RestartApplications=no

DisableWelcomePage=no
DisableFinishedPage=no
ShowLanguageDialog=no

VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
WelcomeLabel1=Welcome to NexaPlay

; Jangan tambah "Click Next to continue." di sini.
; Inno Setup sudah otomatis menambahkan teks lengkap di bawahnya.
WelcomeLabel2=Install NexaPlay on your computer.%n%nA clean and modern launcher experience for your game library.

SetupWindowTitle=NexaPlay Setup
InstallingLabel=Installing NexaPlay. Please wait...

FinishedHeadingLabel=NexaPlay is ready
FinishedLabelNoIcons=Setup has finished installing NexaPlay on your computer.%n%nYou can launch NexaPlay from the Start Menu or desktop shortcut if selected.
FinishedLabel=Setup has finished installing NexaPlay on your computer.%n%nYou can launch NexaPlay from the Start Menu or desktop shortcut if selected.

ButtonNext=&Next
ButtonBack=&Back
ButtonCancel=Cancel
ButtonFinish=&Finish

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
; File aplikasi utama
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Icon aplikasi
Source: "{#MyAppIconFile}"; DestDir: "{app}"; Flags: ignoreversion

; Frame animasi hanya dipakai installer, tidak ikut masuk ke folder aplikasi
Source: "{#MyWizardAnimDir}\frame_*.bmp"; Flags: dontcopy

; Logo kecil untuk ditempel manual di header hitam
Source: "{#MyWizardSmallImageFile}"; Flags: dontcopy

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
const
  { 50 frame untuk 5 detik di 10 FPS }
  FrameCount = 50;

  { 100ms = 10 FPS }
  FrameDelay = 100;

var
  CurrentFrame: Integer;
  AnimationTimer: LongWord;
  CurrentWizardPage: Integer;
  HeaderLogoImage: TBitmapImage;

function SetTimer(hWnd: LongWord; nIDEvent: LongWord; uElapse: LongWord; lpTimerFunc: LongWord): LongWord;
  external 'SetTimer@user32.dll stdcall';

function KillTimer(hWnd: LongWord; nIDEvent: LongWord): Boolean;
  external 'KillTimer@user32.dll stdcall';

function PadFrameNumber(Number: Integer): String;
begin
  if Number < 10 then
    Result := '000' + IntToStr(Number)
  else if Number < 100 then
    Result := '00' + IntToStr(Number)
  else if Number < 1000 then
    Result := '0' + IntToStr(Number)
  else
    Result := IntToStr(Number);
end;

function GetFrameFileName(Number: Integer): String;
begin
  Result := 'frame_' + PadFrameNumber(Number) + '.bmp';
end;

procedure ExtractAnimationFrames;
var
  I: Integer;
begin
  for I := 1 to FrameCount do
  begin
    ExtractTemporaryFile(GetFrameFileName(I));
  end;
end;

procedure LoadAnimationFrame(FrameNumber: Integer);
var
  FramePath: String;
begin
  FramePath := ExpandConstant('{tmp}\' + GetFrameFileName(FrameNumber));

  if FileExists(FramePath) then
  begin
    try
      WizardForm.WizardBitmapImage.Bitmap.LoadFromFile(FramePath);
    except
      { Abaikan frame rusak supaya setup tetap lanjut }
    end;
  end;
end;

procedure AnimationTimerProc(
  hWnd: LongWord;
  uMsg: LongWord;
  idEvent: LongWord;
  dwTime: LongWord
);
begin
  { Animasi loop terus hanya di halaman Welcome dan Finish }
  if (CurrentWizardPage = wpWelcome) or (CurrentWizardPage = wpFinished) then
  begin
    LoadAnimationFrame(CurrentFrame);

    CurrentFrame := CurrentFrame + 1;

    if CurrentFrame > FrameCount then
      CurrentFrame := 1;
  end;
end;

procedure StartAnimation;
begin
  if AnimationTimer = 0 then
  begin
    AnimationTimer := SetTimer(
      WizardForm.Handle,
      0,
      FrameDelay,
      CreateCallback(@AnimationTimerProc)
    );
  end;
end;

procedure StopAnimation;
begin
  if AnimationTimer <> 0 then
  begin
    KillTimer(WizardForm.Handle, AnimationTimer);
    AnimationTimer := 0;
  end;
end;

procedure UpdateHeaderLogoVisibility;
begin
  if HeaderLogoImage <> nil then
  begin
    { Logo kecil hanya tampil di halaman tengah, bukan Welcome/Finish }
    HeaderLogoImage.Visible :=
      not ((CurrentWizardPage = wpWelcome) or (CurrentWizardPage = wpFinished));
  end;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  CurrentWizardPage := CurPageID;

  if (CurrentWizardPage = wpWelcome) or (CurrentWizardPage = wpFinished) then
  begin
    LoadAnimationFrame(CurrentFrame);
    StartAnimation;
  end;

  UpdateHeaderLogoVisibility;
end;

procedure InitializeWizard;
var
  SmallLogoPath: String;
begin
  AnimationTimer := 0;
  CurrentFrame := 1;
  CurrentWizardPage := wpWelcome;

  ExtractAnimationFrames;
  ExtractTemporaryFile('wizard-small.bmp');

  { Sembunyikan small image default bawaan Inno Setup }
  WizardForm.WizardSmallBitmapImage.Visible := False;
  WizardForm.WizardSmallBitmapImage.Width := 0;
  WizardForm.WizardSmallBitmapImage.Height := 0;

  { Warna utama }
  WizardForm.Color := $FFFFFF;
  WizardForm.InnerPage.Color := $FFFFFF;

  { Header atas hitam untuk halaman tengah }
  WizardForm.MainPanel.Color := $000000;

  { Tinggi header supaya title tidak kepotong }
  WizardForm.MainPanel.Height := ScaleY(66);

  { Rapikan posisi title halaman tengah }
  WizardForm.PageNameLabel.Top := ScaleY(10);
  WizardForm.PageNameLabel.Height := ScaleY(24);
  WizardForm.PageNameLabel.Font.Name := 'Segoe UI';
  WizardForm.PageNameLabel.Font.Size := 11;
  WizardForm.PageNameLabel.Font.Style := [fsBold];
  WizardForm.PageNameLabel.Font.Color := $FFFFFF;

  WizardForm.PageDescriptionLabel.Top := ScaleY(33);
  WizardForm.PageDescriptionLabel.Height := ScaleY(20);
  WizardForm.PageDescriptionLabel.Font.Name := 'Segoe UI';
  WizardForm.PageDescriptionLabel.Font.Size := 8;
  WizardForm.PageDescriptionLabel.Font.Color := $D0D0D0;

  { Geser isi halaman tengah mengikuti tinggi header baru }
  WizardForm.InnerNotebook.Top := WizardForm.MainPanel.Top + WizardForm.MainPanel.Height;
  WizardForm.InnerNotebook.Height :=
    WizardForm.Bevel.Top - WizardForm.InnerNotebook.Top;

  { Tempel wizard-small.bmp manual di pojok kanan header hitam }
  SmallLogoPath := ExpandConstant('{tmp}\wizard-small.bmp');

  HeaderLogoImage := TBitmapImage.Create(WizardForm);
  HeaderLogoImage.Parent := WizardForm.MainPanel;
  HeaderLogoImage.AutoSize := True;

  if FileExists(SmallLogoPath) then
  begin
    try
      HeaderLogoImage.Bitmap.LoadFromFile(SmallLogoPath);
    except
    end;
  end;

  { Posisi logo kecil di kanan atas header.
    Kalau terlalu mepet kanan/kiri, ubah ScaleX(16).
    Kalau terlalu naik/turun, ubah perhitungan Top. }
  HeaderLogoImage.Left :=
    WizardForm.MainPanel.Width - HeaderLogoImage.Width - ScaleX(16);

  HeaderLogoImage.Top :=
    (WizardForm.MainPanel.Height - HeaderLogoImage.Height) div 2;

  HeaderLogoImage.BringToFront;

  { Welcome title }
  WizardForm.WelcomeLabel1.Font.Name := 'Segoe UI';
  WizardForm.WelcomeLabel1.Font.Size := 15;
  WizardForm.WelcomeLabel1.Font.Style := [fsBold];
  WizardForm.WelcomeLabel1.Font.Color := $111111;

  { Welcome description }
  WizardForm.WelcomeLabel2.Font.Name := 'Segoe UI';
  WizardForm.WelcomeLabel2.Font.Size := 9;
  WizardForm.WelcomeLabel2.Font.Color := $333333;

  { Finish title }
  WizardForm.FinishedHeadingLabel.Font.Name := 'Segoe UI';
  WizardForm.FinishedHeadingLabel.Font.Size := 15;
  WizardForm.FinishedHeadingLabel.Font.Style := [fsBold];
  WizardForm.FinishedHeadingLabel.Font.Color := $111111;

  { Finish description }
  WizardForm.FinishedLabel.Font.Name := 'Segoe UI';
  WizardForm.FinishedLabel.Font.Size := 9;
  WizardForm.FinishedLabel.Font.Color := $333333;

  { Body text / status install }
  WizardForm.StatusLabel.Font.Name := 'Segoe UI';
  WizardForm.StatusLabel.Font.Size := 9;
  WizardForm.StatusLabel.Font.Color := $333333;

  { Tombol }
  WizardForm.NextButton.Font.Name := 'Segoe UI';
  WizardForm.BackButton.Font.Name := 'Segoe UI';
  WizardForm.CancelButton.Font.Name := 'Segoe UI';

  LoadAnimationFrame(1);
  UpdateHeaderLogoVisibility;
  StartAnimation;
end;

procedure DeinitializeSetup;
begin
  StopAnimation;
end;