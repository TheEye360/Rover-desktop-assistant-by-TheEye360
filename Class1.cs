using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Speech.Synthesis;
using System.Reflection;
using Microsoft.Win32;
using TimerSystemTimers = System.Timers.Timer;

class Rover : Form
{
    private bool doNotDisturb = false;
    private NotifyIcon trayIcon;
    private ContextMenuStrip menu;
    private PictureBox roverImage;
    private SpeechSynthesizer speaker = new SpeechSynthesizer();
    private TimerSystemTimers animationTimer;
    private string[] animations = { "C:\\rover2.gif", "C:\\rover3.gif", "C:\\rover4.gif" };
    private Random random = new Random();
    private bool dragging = false;
    private Point dragCursorPoint;
    private Point dragFormPoint;
    private bool isMainAnimationPlaying = true;
    private TimerSystemTimers returnToRoverTimer;

    public Rover()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.BackColor = Color.Magenta;
        this.TransparencyKey = Color.Magenta;
        this.TopMost = true;
        this.StartPosition = FormStartPosition.Manual;
        this.Location = new Point(100, 100);
        this.Size = new Size(128, 128);
        this.ShowInTaskbar = false;

        roverImage = new PictureBox()
        {
            Image = Image.FromFile("C:\\rover.gif"), // Начинаем с rover.gif
            SizeMode = PictureBoxSizeMode.StretchImage,
            Dock = DockStyle.Fill,
        };
        roverImage.MouseDown += Rover_MouseDown;
        roverImage.MouseMove += Rover_MouseMove;
        roverImage.MouseUp += Rover_MouseUp;
        roverImage.MouseClick += Rover_Click;
        this.Controls.Add(roverImage);

        menu = new ContextMenuStrip();

        var gdiMenu = new ToolStripMenuItem("GDI");
        gdiMenu.DropDownItems.Add("Красный фильтр", null, (s, e) => ApplyScreenFilter(Color.Red));
        gdiMenu.DropDownItems.Add("Зелёный фильтр", null, (s, e) => ApplyScreenFilter(Color.Green));
        gdiMenu.DropDownItems.Add("Синий фильтр", null, (s, e) => ApplyScreenFilter(Color.Blue));

        menu.Items.Add("Гав!", null, (s, e) => Speak("Гав!"));
        menu.Items.Add("Открыть CMD", null, (s, e) => OpenProgram("cmd.exe"));
        menu.Items.Add("Запустить RoverNET", null, (s, e) => OpenProgram("chrome.exe"));
        menu.Items.Add("Открыть Диспетчер задач", null, (s, e) => OpenProgram("taskmgr.exe"));
        menu.Items.Add("Включить режим 'Не беспокоить'", null, ToggleDoNotDisturb);
        menu.Items.Add(gdiMenu);
        menu.Items.Add("Ударить Rover", null, (s, e) => Speak("Ай! Не бей меня!"));
        menu.Items.Add("Выход", null, (s, e) => ExitApp());

        trayIcon = new NotifyIcon()
        {
            Icon = new Icon("C:\\RoverTrayIcon.ico"),
            Visible = true,
            ContextMenuStrip = menu
        };

        // Таймер для проверки каждые 20 секунд
        animationTimer = new TimerSystemTimers(20000); // Проверка каждые 20 секунд
        animationTimer.Elapsed += (s, e) => ChangeAnimation();
        animationTimer.Start();

        // Добавляем в автозагрузку
        AddToStartup();
    }

    private void ApplyScreenFilter(Color filterColor)
    {
        // Create a semi-transparent filter layer
        using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
        {
            // Set the color to filter with a semi-transparent effect
            Color semiTransparentColor = Color.FromArgb(128, filterColor.R, filterColor.G, filterColor.B);
            using (Brush brush = new SolidBrush(semiTransparentColor))
            {
                g.FillRectangle(brush, 0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            }
        }
    }

    private void Rover_MouseDown(object sender, MouseEventArgs e)
    {
        dragging = true;
        dragCursorPoint = Cursor.Position;
        dragFormPoint = this.Location;
    }

    private void Rover_MouseMove(object sender, MouseEventArgs e)
    {
        if (dragging)
        {
            Point diff = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
            this.Location = Point.Add(dragFormPoint, new Size(diff));
        }
    }

    private void Rover_MouseUp(object sender, MouseEventArgs e)
    {
        dragging = false;
    }

    private void Rover_Click(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
            menu.Show(Cursor.Position);
    }

    private void Speak(string text)
    {
        speaker.SpeakAsync(text);
    }

    private void OpenProgram(string program)
    {
        System.Diagnostics.Process.Start(program);
    }

    private void ChangeAnimation()
    {
        // 20% шанс на смену гифки
        if (random.Next(100) < 20)
        {
            // Выбираем случайную анимацию
            int index = random.Next(animations.Length);
            roverImage.Image = Image.FromFile(animations[index]);

            // Создаем таймер для отсчета времени на текущую анимацию
            returnToRoverTimer?.Stop(); // Останавливаем предыдущий таймер, если он был
            returnToRoverTimer = new TimerSystemTimers(5000); // Ожидаем завершения анимации (поставим 5 секунд)
            returnToRoverTimer.Elapsed += (s, e) =>
            {
                roverImage.Image = Image.FromFile("C:\\rover.gif"); // Возвращаем на основную гифку после завершения анимации
            };
            returnToRoverTimer.Start();
        }
        else
        {
            // По умолчанию используем первое изображение (rover.gif)
            roverImage.Image = Image.FromFile("C:\\rover.gif");
        }
    }

    private void ToggleDoNotDisturb(object sender, EventArgs e)
    {
        doNotDisturb = !doNotDisturb;
        menu.Items[4].Text = doNotDisturb ? "Выключить режим 'Не беспокоить'" : "Включить режим 'Не беспокоить'";
    }

    private void ExitApp()
    {
        trayIcon.Visible = false;
        Application.Exit();
    }

    // Метод для добавления приложения в автозагрузку
    private void AddToStartup()
    {
        string appName = "Rover";
        string appPath = Assembly.GetExecutingAssembly().Location;

        // Получаем ключ автозагрузки из реестра
        string registryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(registryKeyPath, writable: true);

        // Проверяем, существует ли ключ автозагрузки
        if (registryKey.GetValue(appName) == null)
        {
            // Добавляем путь к приложению в реестр
            registryKey.SetValue(appName, appPath);
        }
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new Rover());
    }
}
