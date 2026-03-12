using CatfortSound.SoundEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Windows.Data;

namespace CatfortSound.ViewModels
{
    public class EntryViewModel<T>
    {
        private ObservableCollection<T> _EntryList;
        public IEditableCollectionView EntryView { get; private set; }
        public EntryViewModel()
        {
            _EntryList = new ObservableCollection<T>();
        }

        public EntryViewModel(System.Collections.IList startList)
        {
            _EntryList = (ObservableCollection<T>)startList;
        }

        public void BindToDataGrid(DataGrid grid)
        {
            grid.DataContext = this;
            EntryView = CollectionViewSource.GetDefaultView(this._EntryList) as IEditableCollectionView;
        }
        public ObservableCollection<T> Entries
        { 
            get { return _EntryList; }
            set { _EntryList = value; }
        }

        public void Clear()
        {
            _EntryList.Clear();
        }

        public void PasteList(IList<T>itemsToCopy, int startIdx)
        {
            if (EntryView.IsAddingNew)
            {
                //don't allow pasting when adding new
                return;
            }
            int idx = startIdx;
            foreach (var item in itemsToCopy)
            {
                if (idx >= _EntryList.Count)
                {
                    _EntryList.Add(item.DeepClone());
                }
                else
                {
                    _EntryList.RemoveAt(idx);
                    _EntryList.Insert(idx, item.DeepClone());
                }
                idx++;
            }
        }

        private ICommand _Updater;

        public ICommand UpdateCommand
        {
            get
            {
                if (_Updater == null)
                    _Updater = new Updater();
                return _Updater;
            }
            set
            {
                _Updater = value;
            }
        }

        private class Updater : ICommand
        {
            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;

            public void Execute(object parameter)
            {
                // Code implementation for execution
            }
        }
    }
}
