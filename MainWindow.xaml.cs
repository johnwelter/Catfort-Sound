using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CatfortSound.SoundEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.MusicTheory;
using Ownaudio;
using Ownaudio.Sources;

namespace CatfortSound;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{

    public Stopwatch Time = new Stopwatch();
    private double m_deltaTime;
    private double m_timeLastFrame;
    private double m_frameTimer;
    private double m_clockTimer;
    private APU AudioProcessor = new APU();
    private Sequencer NoteRoll;

    const double frameTime = 16.67;

    public bool AudioActive = false;

    BackgroundWorker audioLoopWorker;

    public bool testDump = true;

    public MainWindow()
    {
        InitializeComponent();
        NoteRoll = new Sequencer(AudioProcessor);
        InitAudioUpdate();
        CompositionTarget.Rendering += RenderUpdate;

        sq1_vol.Value = 1;
        sq2_vol.Value = 1;
        tri_vol.Value = 1;
        noi_vol.Value = 1;
        dmc_vol.Value = 1;
    }

    protected void RenderUpdate(object? sender, EventArgs? e)
    {
        AudioProcessor.UpdateVolume(Mixer.SQUARE_1, (float)sq1_vol.Value);
        AudioProcessor.UpdateVolume(Mixer.SQUARE_2, (float)sq2_vol.Value);
        AudioProcessor.UpdateVolume(Mixer.TRIANGLE, (float)tri_vol.Value);
        AudioProcessor.UpdateVolume(Mixer.NOISE, (float)noi_vol.Value);
        AudioProcessor.UpdateVolume(Mixer.DMC, (float)dmc_vol.Value);
        
    }

    private void InitAudioUpdate()
    {
        AudioActive = true;
        audioLoopWorker = new BackgroundWorker(); 
        audioLoopWorker.WorkerReportsProgress = true;
        audioLoopWorker.WorkerSupportsCancellation = true;
        audioLoopWorker.DoWork += DoAudioLoop;
        audioLoopWorker.RunWorkerAsync();
    }

    
    void DoAudioLoop(object? sender, DoWorkEventArgs? e)
    {
        Time.Start();
        
        while (AudioActive)
        {
            double timeThisFrame = Time.ElapsedMilliseconds;
            m_deltaTime = timeThisFrame - m_timeLastFrame;
            m_frameTimer += m_deltaTime;
            m_clockTimer += m_deltaTime;
            if(m_frameTimer >= frameTime)
            {
                m_frameTimer -= frameTime;
                DoFrameUpdate();
            }

            if(AudioProcessor.Update(m_deltaTime, testDump))
            {
                m_timeLastFrame = timeThisFrame;
            }
        }
        Time.Reset();
    }

    void DoFrameUpdate()
    {
        int dirtyChannels = NoteRoll.TickSequence();
 
        AudioProcessor.FrameTick();

        if ((dirtyChannels & 128) != 0 && testDump)
        {
            testDump = false;
            Debug.WriteLine("export!");
            AudioProcessor.OutputSound();
        }
    }
}