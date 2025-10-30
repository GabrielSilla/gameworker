using GamingWorkerService;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SharpDX.XInput;

public class GameLauncherForm : Form
{
    private Image[] coverImages;
    private int selectedIndex = 0;
    private string[] executables;
    private string drivePath;
    private Controller controller;
    private System.Windows.Forms.Timer inputTimer;

    private int stickCooldown = 0; // contador interno para cooldown
    private const int StickDelay = 5; // ticks para aceitar próximo movimento do stick
    private const short StickThreshold = 8000; // sensibilidade do analógico

    public GameLauncherForm(string drivePath)
    {
        this.drivePath = drivePath;

        // Lê o run.txt (cada linha é um executável)
        var runFile = Path.Combine(drivePath, "run.txt");
        executables = File.ReadAllLines(runFile)
                          .Where(l => !string.IsNullOrWhiteSpace(l))
                          .ToArray();

        if (executables.Length == 0)
        {
            MessageBox.Show("Nenhum jogo encontrado no run.txt");
            Application.Exit();
            return;
        }

        // Configura janela
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        BackColor = Color.Black;
        KeyPreview = true;
        Cursor.Hide();

        // Carrega imagens das capas
        coverImages = new Image[executables.Length];
        for (int i = 0; i < executables.Length; i++)
        {
            var coverPath = Path.Combine(drivePath, "resources", $"cover{i + 1}.jpg");
            coverImages[i] = File.Exists(coverPath) 
                             ? Image.FromFile(coverPath) 
                             : CriarPlaceholder($"Jogo {i + 1}");
        }

        KeyDown += OnKeyDown;

        // Configura timer para ler o controle XInput
        controller = new Controller(UserIndex.One);
        inputTimer = new System.Windows.Forms.Timer { Interval = 100 };
        inputTimer.Tick += (s, e) => LerControle();
        inputTimer.Start();

        Paint += OnPaint;
    }

    private void OnPaint(object sender, PaintEventArgs e)
    {
        int screenWidth = ClientSize.Width;
        int screenHeight = ClientSize.Height;
        int count = coverImages.Length;
        int coverWidth = screenWidth / count;

        for (int i = 0; i < count; i++)
        {
            Image img = coverImages[i];
            float scale = (float)screenHeight / img.Height;
            int scaledWidth = (int)(img.Width * scale);

            // Se a imagem for mais larga que a coluna, cortamos horizontalmente
            int cropX = 0;
            if (scaledWidth > coverWidth)
            {
                int excess = scaledWidth - coverWidth;
                cropX = (int)(excess / (2 * scale)); // convertendo para coordenadas da imagem original
                scaledWidth = coverWidth;
            }

            using (var bmp = new Bitmap(scaledWidth, screenHeight))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(img,
                            new Rectangle(0, 0, scaledWidth, screenHeight),
                            new Rectangle(cropX, 0, (int)(scaledWidth / scale), img.Height),
                            GraphicsUnit.Pixel);

                float brightness = (i == selectedIndex) ? 1.5f : 0.5f;
                using (var adjusted = AjustarBrilho(bmp, brightness))
                {
                    e.Graphics.DrawImage(adjusted, new Rectangle(i * coverWidth, 0, coverWidth, screenHeight));
                }
            }
        }
    }

    private void LerControle()
    {
if (!controller.IsConnected) return;
    var state = controller.GetState();
    var buttons = state.Gamepad.Buttons;

    // Direcional DPad
    if (buttons.HasFlag(GamepadButtonFlags.DPadRight))
    {
        selectedIndex = (selectedIndex + 1) % executables.Length;
        Invalidate();
    }
    else if (buttons.HasFlag(GamepadButtonFlags.DPadLeft))
    {
        selectedIndex = (selectedIndex - 1 + executables.Length) % executables.Length;
        Invalidate();
    }

    // Analógico esquerdo
    short thumbLX = state.Gamepad.LeftThumbX;
    if (stickCooldown > 0)
    {
        stickCooldown--;
    }
    else
    {
        if (thumbLX > StickThreshold)
        {
            selectedIndex = (selectedIndex + 1) % executables.Length;
            Invalidate();
            stickCooldown = StickDelay;
        }
        else if (thumbLX < -StickThreshold)
        {
            selectedIndex = (selectedIndex - 1 + executables.Length) % executables.Length;
            Invalidate();
            stickCooldown = StickDelay;
        }
    }

    // A -> executar, B -> cancelar
    if (buttons.HasFlag(GamepadButtonFlags.A))
    {
        ExecutarJogo();
    }
    else if (buttons.HasFlag(GamepadButtonFlags.B))
    {
        Application.Exit();
    }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Right:
                selectedIndex = (selectedIndex + 1) % executables.Length;
                Invalidate();
                break;
            case Keys.Left:
                selectedIndex = (selectedIndex - 1 + executables.Length) % executables.Length;
                Invalidate();
                break;
            case Keys.Enter:
            case Keys.A:
                ExecutarJogo();
                break;
            case Keys.Escape:
            case Keys.B:
                Application.Exit();
                break;
        }
    }

    private void ExecutarJogo()
    {
        try
        {
            var exeRelPath = executables[selectedIndex].Trim();
            var exeFullPath = Path.Combine(drivePath, exeRelPath);

            var argsFilePath = Path.Combine(drivePath, "args.txt");
            var args = File.Exists(argsFilePath) ? File.ReadAllText(argsFilePath).Trim() : "";

            if (!File.Exists(exeFullPath))
            {
                MessageBox.Show($"Arquivo não encontrado:\n{exeFullPath}");
                return;
            }

            TaskManager.RunProcess(exeFullPath, args);
            Application.Exit();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Erro ao iniciar o jogo:\n" + ex.Message);
        }
    }

    private Bitmap CriarPlaceholder(string texto)
    {
        Bitmap bmp = new Bitmap(400, 400);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Gray);
            using (var font = new Font("Arial", 24, FontStyle.Bold))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(texto, font, Brushes.White, new RectangleF(0, 0, 400, 400), sf);
            }
        }
        return bmp;
    }

    private Image AjustarBrilho(Image img, float fator)
    {
        Bitmap bmp = new Bitmap(img.Width, img.Height);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            float[][] ptsArray = {
                new float[] { fator, 0, 0, 0, 0 },
                new float[] { 0, fator, 0, 0, 0 },
                new float[] { 0, 0, fator, 0, 0 },
                new float[] { 0, 0, 0, 1, 0 },
                new float[] { 0, 0, 0, 0, 1 }
            };
            using (var attrs = new System.Drawing.Imaging.ImageAttributes())
            {
                attrs.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix(ptsArray));
                g.DrawImage(img, new Rectangle(0, 0, bmp.Width, bmp.Height),
                    0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attrs);
            }
        }
        return bmp;
    }
}
