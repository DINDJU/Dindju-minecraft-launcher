Imports System
Imports System.Linq
Imports System.Net.Http
Imports System.Windows.Forms
Imports System.Threading.Tasks
Imports System.Diagnostics
Imports Newtonsoft.Json

Imports CmlLib.Core            ' MinecraftPath, CMLauncher
Imports CmlLib.Core.Auth       ' MSession
Imports CmlLib.Core.Version    ' MLaunchOption

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Public Class Form1
    Inherits System.Windows.Forms.Form

    ' --- CONFIGURATION ---
    Private ReadOnly serverIp As String = "91.197.5.218"
    Private ReadOnly serverPort As Integer = 26707
    ' ----------------------

    Public Sub New()
        InitializeComponent()
        Control.CheckForIllegalCrossThreadCalls = False
        txtUsername.Text = "Pseudo"
        txtUsername.ForeColor = Color.Gray
    End Sub

    Private Async Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Await PopulateVersionListAsync()
    End Sub


    Private Async Function PopulateVersionListAsync() As Task
        cmbVersion.Items.Clear()
        cmbVersion.Items.Add("Chargement...")
        cmbVersion.SelectedIndex = 0

        Try
            Const manifestUrl As String = "https://launchermeta.mojang.com/mc/game/version_manifest.json"
            Using client As New HttpClient()
                Dim json = Await client.GetStringAsync(manifestUrl)
                Dim manifest = JsonConvert.DeserializeObject(Of VersionManifest)(json)
                Dim releases = manifest.Versions _
                    .Where(Function(v) v.Type = "release" AndAlso CompareVersionIds(v.Id, "1.8.8") >= 0) _
                    .Select(Function(v) v.Id) _
                    .ToList()
                releases.Sort(AddressOf CompareVersionIds)

                cmbVersion.Items.Clear()
                For Each vid In releases
                    cmbVersion.Items.Add(vid)
                Next
                cmbVersion.SelectedIndex = cmbVersion.Items.Count - 1
            End Using

        Catch ex As Exception
            Debug.WriteLine($"[ERROR] chargement versions : {ex.Message}")
            cmbVersion.Items.Clear()
            cmbVersion.Items.Add("1.8.8")
            cmbVersion.SelectedIndex = 0
        End Try
    End Function

    Private Function CompareVersionIds(a As String, b As String) As Integer
        Dim aSeg = a.Split("."c).Select(Function(s) If(Integer.TryParse(s, Nothing), Integer.Parse(s), 0)).ToArray()
        Dim bSeg = b.Split("."c).Select(Function(s) If(Integer.TryParse(s, Nothing), Integer.Parse(s), 0)).ToArray()
        Dim length = Math.Max(aSeg.Length, bSeg.Length)
        For i = 0 To length - 1
            Dim av = If(i < aSeg.Length, aSeg(i), 0)
            Dim bv = If(i < bSeg.Length, bSeg(i), 0)
            If av < bv Then Return -1
            If av > bv Then Return 1
        Next
        Return 0
    End Function

    Private Function CreateOfflineSession(playerName As String) As MSession
        Return New MSession() With {
            .Username = playerName,
            .UUID = playerName,
            .AccessToken = "0",
            .UserType = "legacy"
        }
    End Function

    Private Async Sub LaunchMinecraftGame()
        Dim versionToLaunch = cmbVersion.SelectedItem?.ToString()
        If String.IsNullOrEmpty(versionToLaunch) Then
            MessageBox.Show("Version invalide.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
            btnLaunch.Enabled = True
            Return
        End If

        Dim path As New MinecraftPath(".Dindjulauncher")
        Dim launcher As New CMLauncher(path)

        Try
            Dim launchOption As New MLaunchOption() With {
                .MaximumRamMb = 2048,
                .Session = CreateOfflineSession(txtUsername.Text),
                .ServerIp = serverIp,
                .ServerPort = serverPort
            }

            Debug.WriteLine($"[CMLIB] Lancement Minecraft {versionToLaunch}...")
            Dim proc = Await launcher.CreateProcessAsync(versionToLaunch, launchOption)
            proc.Start()

            Hide()
            Debug.WriteLine("[Launcher] Minecraft lancé.")
        Catch ex As Exception
            Debug.WriteLine($"[ERROR] {ex}")
            MessageBox.Show($"Erreur de lancement :{Environment.NewLine}{ex.Message}", "Erreur Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error)
            btnLaunch.Enabled = True
        End Try
    End Sub

    Private Sub btnLaunch_Click(sender As Object, e As EventArgs) Handles btnLaunch.Click
        If String.IsNullOrWhiteSpace(txtUsername.Text) OrElse txtUsername.Text = "Pseudo" Then
            MessageBox.Show("Veuillez entrer un pseudo valide !", "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        btnLaunch.Enabled = False
        Task.Run(AddressOf LaunchMinecraftGame)
    End Sub

    Private Sub txtUsername_Enter(sender As Object, e As EventArgs) Handles txtUsername.Enter
        If txtUsername.Text = "Pseudo" Then
            txtUsername.Text = ""
            txtUsername.ForeColor = Color.Black
        End If
    End Sub

    Private Sub txtUsername_Leave(sender As Object, e As EventArgs) Handles txtUsername.Leave
        If String.IsNullOrWhiteSpace(txtUsername.Text) Then
            txtUsername.Text = "Pseudo"
            txtUsername.ForeColor = Color.Gray
        End If
    End Sub
End Class

' Toutes les classes déclarées après Form1 pour éviter les erreurs du Designer
Public Class VersionManifest
    Public Property Versions As List(Of VersionInfo)
End Class

Public Class VersionInfo
    Public Property Id As String
    Public Property Type As String
    Public Property Url As String
End Class
