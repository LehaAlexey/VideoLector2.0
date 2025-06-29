using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using VideoLecturer.Core.Models;
using WpfApplication.Core.Models;
using WpfApplication.Core.Services;
using WpfApplication.Views;
using static WpfApplication.Core.ViewModels.ProjectViewModel;

namespace WpfApplication.Core.ViewModels
{
    public class LectorManagerViewModel : BaseViewModel
    {
        private readonly ILectorManager _lectorManager;
        private LectorProfile _selectedLector;

        public ObservableCollection<LectorProfile> Lectors { get; } = new();
        public LectorProfile SelectedLector
        {
            get => _selectedLector;
            set => SetProperty(ref _selectedLector, value);
        }

        public ICommand AddLectorCommand { get; }
        public ICommand EditLectorCommand { get; }
        public ICommand DeleteLectorCommand { get; }

        public LectorManagerViewModel(ILectorManager lectorManager)
        {
            _lectorManager = lectorManager;

            // Загрузка лекторов
            foreach (var lector in _lectorManager.AllLectors)
            {
                Lectors.Add(lector);
            }

            // Команды
            AddLectorCommand = new RelayCommand(AddLector);
            EditLectorCommand = new RelayCommand(EditLector, CanEditDelete);
            DeleteLectorCommand = new RelayCommand(DeleteLector, CanEditDelete);
        }

        private bool CanEditDelete() => SelectedLector != null;

        private void AddLector()
        {
            var window = new AddEditLectorWindow(_lectorManager)
            {
                Owner = System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
            };

            if (window.ShowDialog() == true)
            {
                // Просто обновляем всю коллекцию
                Lectors.Clear();
                foreach (var lector in _lectorManager.AllLectors)
                {
                    Lectors.Add(lector);
                }
            }
        }

        private void EditLector()
        {
            if (SelectedLector == null) return;

            var window = new AddEditLectorWindow(_lectorManager, SelectedLector)
            {
                Owner = System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
            };

            if (window.ShowDialog() == true)
            {
                // Создаем новый экземпляр с обновленными данными
                var updatedLector = _lectorManager.GetLectorById(SelectedLector.Id);
                var index = Lectors.IndexOf(SelectedLector);
                Lectors[index] = updatedLector; // Заменяем элемент полностью
                SelectedLector = updatedLector; // Обновляем выбранный элемент
            }
        }

        private void DeleteLector()
        {
            if (SelectedLector == null) return;

            if (System.Windows.MessageBox.Show("Удалить выбранного лектора?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var lectorToRemove = SelectedLector;
                _lectorManager.RemoveLector(lectorToRemove.Id);
                Lectors.Remove(lectorToRemove);
                SelectedLector = null;
            }
        }
    }
}