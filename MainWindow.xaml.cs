using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Media;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.RightsManagement;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Xml;
using CatfortSound.SoundEngine;
using CatfortSound.ViewModels;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.MusicTheory;
using Microsoft.Win32;
using Newtonsoft.Json;
using Ownaudio;
using Ownaudio.Sources;

namespace CatfortSound;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{

    public Stopwatch Time = new Stopwatch();
    private double m_deltaTime;
    private double m_timeLastFrame;
    private double m_frameTimer;
    private double m_clockTimer;
    private APU AudioProcessor = new();
    private Sequencer Sequencer;

    const double frameTime = 16.67;

    string currentFileName = "puzzleBank";
    string currentPath = "";
    string exportPath = "";

    System.Collections.IList clipboard;

    bool mouseDown;
    FrameworkElement? cellToEdit;
    Vector mousePosRecord = new();
    bool mouseHeld = false;

    public List<Slider> volumeSliders = new List<Slider>();
    public object[] ViewModels = new object[5]; 
    public DataGrid[] TrackerLists = new DataGrid[5];
    public DataGrid[] LoopLists = new DataGrid[5];

    private bool audioActive = false;
    public bool AudioActive
    {
        get { return audioActive; }
        set
        {
            if(audioActive != value)
            {
                audioActive = value;
                OnPropertyChanged("PlayActive");
                OnPropertyChanged("StopActive");
            }
        }
    }

    public bool IsPlayEnabled
    {
        get { return !audioActive; }
    }

    public bool IsStopEnabled
    {
        get { return audioActive; }

    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged([CallerMemberName]string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    BackgroundWorker audioLoopWorker;

    public bool testDump = true;


    public MainWindow()
    {
        InitializeComponent();
        Sequencer = new Sequencer(AudioProcessor);
        InitAudioUpdate();
        CompositionTarget.Rendering += RenderUpdate;

        sq1_vol.Value = 1;
        sq2_vol.Value = 1;
        tri_vol.Value = 1;
        noi_vol.Value = 1;
        dmc_vol.Value = 1;

        volumeSliders.Add(sq1_vol);
        volumeSliders.Add(sq2_vol);
        volumeSliders.Add(tri_vol);
        volumeSliders.Add(noi_vol);
        volumeSliders.Add(dmc_vol);

        ViewModels[0] = new EntryViewModel<PulseEntry>(Sequencer.SongChart.Channels[0] as System.Collections.IList);
        ViewModels[1] = new EntryViewModel<PulseEntry>(Sequencer.SongChart.Channels[1] as System.Collections.IList);
        ViewModels[2] = new EntryViewModel<OscEntry>(Sequencer.SongChart.Channels[2] as System.Collections.IList);
        ViewModels[3] = new EntryViewModel<NoiseEntry>(Sequencer.SongChart.Channels[3] as System.Collections.IList);
        ViewModels[4] = new EntryViewModel<DMCEntry>(Sequencer.SongChart.Channels[4] as System.Collections.IList);

        LoopLists[0] = p1Subloops;
        LoopLists[1] = p2Subloops;
        LoopLists[2] = tSubloops;
        LoopLists[3] = nSubloops;
        LoopLists[4] = dmcSubloops;

        TrackerLists[0] = p1Grid;
        TrackerLists[1] = p2Grid;
        TrackerLists[2] = tGrid;
        TrackerLists[3] = nGrid;
        TrackerLists[4] = dmcGrid;

        HookupMVVM();

    }

    protected void RenderUpdate(object? sender, EventArgs? e)
    {
        for(int i = 0; i < volumeSliders.Count; i++)
        {
            AudioProcessor.UpdateVolume(i, (float)volumeSliders[i].Value);
        }

        play_button.IsEnabled = IsPlayEnabled;
        stop_button.IsEnabled = IsStopEnabled;

        bool use8 = use8Toggle.IsChecked ?? false;
        tempoBaseLabel.Content = use8? "8th" : "32nd";

    }

    private void InitAudioUpdate()
    {
        audioLoopWorker = new BackgroundWorker(); 
        audioLoopWorker.WorkerReportsProgress = true;
        audioLoopWorker.WorkerSupportsCancellation = true;
        audioLoopWorker.DoWork += DoAudioLoop;
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
            if (m_frameTimer >= frameTime)
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
        int dirtyChannels = Sequencer.TickSequence();
 
        AudioProcessor.FrameTick();

        if ((dirtyChannels & 128) != 0 && testDump)
        {
            testDump = false;
            Debug.WriteLine("export!");
            AudioProcessor.OutputSound();
        }
    }

    void HookupMVVM()
    {
        for(int i = 0; i < ViewModels.Length; i++) 
        {
            MethodInfo BindToGrid = ViewModels[i].GetType().GetMethod("BindToDataGrid");
            BindToGrid.Invoke(ViewModels[i], new object[] { TrackerLists[i] });

            LoopLists[i].ItemsSource = Sequencer.SongChart.Subloops[i];
        }
    }

    private void play_button_Click(object sender, RoutedEventArgs e)
    {
        AudioActive = true;
        AudioProcessor.ResetChannels();
        Sequencer.Reset();
        Sequencer.SetTempo(int.Parse(tempo.Text), use8Toggle.IsChecked);
        
        m_deltaTime = 0;
        m_frameTimer = 0;
        m_clockTimer = 0;
        m_timeLastFrame = 0;
        testDump = true;
        audioLoopWorker.RunWorkerAsync();
    }

    private void stop_button_Click(object sender, RoutedEventArgs e)
    {
        AudioActive = false;
    }

    private void Tempo_Validate(object sender, TextCompositionEventArgs e)
    {
        Regex regex = new Regex("[^0-9]+");
        e.Handled = regex.IsMatch(e.Text);
    }

    private void Toolbar_New(object sender, RoutedEventArgs e)
    {
        MessageBoxResult confirmOpen = MessageBox.Show("Starting a new file will close the current file. Any unsaved changes will be lost.", "Confirmation", MessageBoxButton.OKCancel);

        if (confirmOpen == MessageBoxResult.OK)
        {
            Sequencer.ClearSequencer();

            p1Grid.Items.Refresh();
            p1Subloops.Items.Refresh();
            p2Grid.Items.Refresh();
            tGrid.Items.Refresh();
            nGrid.Items.Refresh();
            dmcGrid.Items.Refresh();
            TestGrid.Items.Refresh();

            currentFileName = "newSong";
            currentPath = "";
            exportPath = "";
        }

    }

    private void Toolbar_Open(object sender, RoutedEventArgs e)
    {
        MessageBoxResult confirmOpen = MessageBox.Show("Opening another file will close the current file. Any unsaved changes will be lost.", "Confirmation", MessageBoxButton.OKCancel);

        if (confirmOpen == MessageBoxResult.OK)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CFS (*.cfs)|*.cfs";
            if (openFileDialog.ShowDialog() == true)
            {
                //string json = System.IO.File.ReadAllText(openFileDialog.FileName);
                byte[] fileByte = System.IO.File.ReadAllBytes(openFileDialog.FileName);

                Sequencer.SongChart.ReadByteData(fileByte);
                Sequencer.Reset();

                currentPath = openFileDialog.FileName;
                currentFileName = openFileDialog.SafeFileName;
                currentFileName = currentFileName.Remove(currentFileName.Length - 4, 4);
            }
        }
    }
    private void Toolbar_Save(object sender, RoutedEventArgs e)
    {
        DoSave(true);
    }

    private void Toolbar_SaveAs(object sender, RoutedEventArgs e)
    {
        DoSave(false);
    }

    private void DoSave(bool useCurrent)
    {
        bool? doSave = true;
        if (!useCurrent || currentPath == "")
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = currentFileName;
            saveFileDialog.Filter = "CFS (*.cfs)|*.cfs";
            doSave = saveFileDialog.ShowDialog();
            if (doSave == true)
            {

                currentPath = saveFileDialog.FileName;
                exportPath = "";
                currentFileName = saveFileDialog.SafeFileName;
                currentFileName = currentFileName.Remove(currentFileName.Length - 4, 4);

            }

        }

        if (doSave == true)
        {
            //string json = JsonConvert.SerializeObject(NoteRoll.seqChart, Newtonsoft.Json.Formatting.Indented);
            //System.IO.File.WriteAllText(currentPath, json);
            //different version, more manual - reads and print bytes directly for the roll system
            byte[] chartBytes = Sequencer.SongChart.GetFullByteData();
            System.IO.File.WriteAllBytes(currentPath, chartBytes);
            
        }

    }


    private void Toolbar_Export(object sender, RoutedEventArgs e)
    {
        //string expPath = "";
        //string expTables = "";
        //string expSprites = "";

        //expPath = currentFileName;
        //expTables = currentFileName + "Tables";
        //expSprites = currentFileName + "Sprites";

        //puzzleSolutionCache.Clear();

        //SaveFileDialog saveFileDialog = new SaveFileDialog();
        //saveFileDialog.FileName = expPath;
        //saveFileDialog.Filter = "ASM (*.asm)|*.asm";
        //if (saveFileDialog.ShowDialog() == true)
        //{
        //    exportPath = saveFileDialog.FileName;
        //    //do ASM export
        //    String output = "";
        //    foreach (Puzzle p in bankObserver.observablePuzzles)
        //    {

        //        output += createMapData(p);
        //        output += "\n";
        //        output += createNTData(p);
        //        output += "\n";
        //        output += createNameData(p);
        //        output += "\n\n";
        //    }

        //    File.WriteAllText(exportPath, output);
        //}

        //saveFileDialog.FileName = expTables;
        //saveFileDialog.Filter = "ASM (*.asm)|*.asm";
        //if (saveFileDialog.ShowDialog() == true)
        //{
        //    exportPath = saveFileDialog.FileName;
        //    //do ASM export
        //    String puzzleTable = "";
        //    String puzzleNames = "";
        //    for (int i = 0; i < bankObserver.observablePuzzles.Count; i++)
        //    {
        //        Puzzle p = bankObserver.observablePuzzles[i];
        //        String name = currentFileName + "_" + p.name.Replace(' ', '_');
        //        if (i % 9 == 0)
        //        {
        //            puzzleTable += "  .word " + name;
        //            puzzleNames += "  .word " + name + "Name";
        //        }
        //        else
        //        {
        //            puzzleTable += ", " + name;
        //            puzzleNames += ", " + name + "Name";
        //            if (i % 9 == 8)
        //            {
        //                puzzleTable += "\n";
        //                puzzleNames += "\n";
        //            }

        //        }

        //    }
        //    String output = puzzleTable + "\n" + puzzleNames;
        //    File.WriteAllText(exportPath, output);


        //}

        //saveFileDialog.FileName = expSprites;
        //saveFileDialog.Filter = "CHR (*.chr)|*.chr";
        //if (saveFileDialog.ShowDialog() == true)
        //{
        //    exportPath = saveFileDialog.FileName;
        //    //do bin export

        //    //16 bytes per tile, 4 tiles per puzzle, 27 puzzles

        //    byte[] solutionCHR = new byte[3840]; //everyting but 16 tiles
        //    for (int i = 0; i < solutionCHR.Length; i++)
        //    {
        //        solutionCHR[i] = 0;
        //    }

        //    for (int puzzleIdx = 0; puzzleIdx < puzzleSolutionCache.Count; puzzleIdx++)
        //    {
        //        byte[] puzzle = puzzleSolutionCache[puzzleIdx];
        //        int puzzleOffset = puzzleIdx * 64;

        //        int twoBytesPerRow = ((puzzle.Length / 5) & 1) ^ 1;

        //        for (int byteIdx = 0; byteIdx < puzzle.Length; byteIdx++)
        //        {
        //            int colOffset = (((byteIdx & 16) << 1) + ((byteIdx & 1) << 4)) * twoBytesPerRow;

        //            int rowIndexOffset = byteIdx >> twoBytesPerRow;

        //            int rowOffset = (rowIndexOffset) & 7;
        //            byte tileByte = puzzle[byteIdx];
        //            int finalIdx = puzzleOffset + rowOffset + colOffset;
        //            solutionCHR[finalIdx] = tileByte;
        //            solutionCHR[finalIdx + 8] = tileByte;
        //        }

        //    }

        //    File.WriteAllBytes(exportPath, solutionCHR);
        //}


    }
    private void Toolbar_About(object sender, RoutedEventArgs e)
    {

    }

    private void Copy(object sender, KeyEventArgs e)
    {
        if (sender is not DataGrid dataGrid) { return; }

        if (dataGrid.SelectedIndex == -1) { return; }

        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (!(e.Key == Key.C || e.Key == Key.V))
            {
                return;
            }
        }

        // to make things simpler with generics, we'll do some grody reflection work
        if (e.Key == Key.C)
        {
            Type genType = dataGrid.SelectedItems[0].GetType();
            if (!genType.IsSubclassOf(typeof(SequenceEntry))) { return; }

            MethodInfo copyMethod = GetType().GetMethod("CopyListToClipboard");
            MethodInfo genericCopy = copyMethod.MakeGenericMethod(genType);
            genericCopy.Invoke(this, new object[] {dataGrid.SelectedItems});
        }
        else if (e.Key == Key.V)
        {
            if (clipboard == null || clipboard.Count <= 0) { return; }

            MethodInfo pasteMethod = dataGrid.DataContext.GetType().GetMethod("PasteList");
            pasteMethod.Invoke(dataGrid.DataContext, new object[] { clipboard, dataGrid.SelectedIndex });
        }
    }

    public void CopyListToClipboard<T>(System.Collections.IList inList)
    {
        clipboard?.Clear();
        clipboard = new List<T>();
        foreach (var item in inList)
        {
            clipboard.Add(((T)item).DeepClone());
        }
    }

    private void BeginCellEdit(object sender, DataGridPreparingCellForEditEventArgs e)
    {
        Debug.WriteLine(e.EditingElement.ToString());
        cellToEdit = e.EditingElement;
    }

    private void EndCellEdit(object sender, DataGridCellEditEndingEventArgs e)
    {
        cellToEdit = null;
    }

    //private void p1Grid_MouseMove(object sender, MouseEventArgs e)
    //{
    //    if(cellToEdit == null) { return; }
    //    int val = cellToEdit switch
    //    {
    //        ComboBox => ((ComboBox)cellToEdit).SelectedIndex,
    //        TextBox _ => int.Parse(((TextBox)cellToEdit).Text),
    //        _ => throw new NotImplementedException(),
    //    };

    //    Point position = e.GetPosition(this);
    //    Vector posVec = new Vector(position.X, position.Y);

    //    if(mouseDown)
    //    {
    //        Vector diff = (posVec - mousePosRecord);
    //        double yChange = diff.Y;
    //        val += (int)(yChange / 10.0);
    //        if (cellToEdit is ComboBox comboBox)
    //        {
    //            comboBox.SelectedIndex = val;
    //        }
    //        else if (cellToEdit is TextBox textBox)
    //        {
    //            textBox.Text = val.ToString();
    //        }
    //    }

    //    mousePosRecord = posVec;
    //}

    //private void p1Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    //{
    //    mouseDown = true;
    //    Debug.WriteLine("down");
    //}

    //private void p1Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    //{
    //    mouseDown = false;
    //    Debug.WriteLine("up");
    //}
}