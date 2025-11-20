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
    }

    protected void RenderUpdate(object? sender, EventArgs? e)
    {
     
        
    }

    private void InitAudioUpdate()
    {
        //AudioProcessor.SetOscilatorEffect(new VolEffect(new byte[] { 7, 8, 9, 10, 11, 12, 13, 14, 15, 15, 15, 14, 14, 14, 13, 13, 13, 12, 12, 12, 11, 11, 11, 10, 10, 10, 9, 9, 9, 8, 8, 8, 7, 7, 7, 6, 6, 6, 5, 5, 5, 4, 4, 4, 3, 3, 3, 2, 2, 2, 1, 1, 1, 0 }), Mixer.SQUARE_1);
        //AudioProcessor.SetOscilatorEffect(new VolEffect(new byte[] { 15, 15, 15, 11, 11, 11, 7, 7, 7, 5, 5, 5 }), Mixer.SQUARE_2);
        //AudioProcessor.SetOscilatorEffect(new VolEffect(new byte[] { 15, 15, 15, 11, 7, 0 }), Mixer.NOISE);

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