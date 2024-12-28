using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace WpfApp5
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private Contact selectedContact;
        private string addEditButtonContent;
        private bool isAddEditEnabled;
        
        public MainViewModel()
        {
            Contacts = new ObservableCollection<Contact>();
            SelectedContact = new Contact();
            AddEditButtonContent = "Добавить";


            AddEditContactCommand = new RelayCommand(AddOrEditContact, CanAddOrEditContact);
            EditContactCommand = new RelayCommand(EditContact, CanEditContact);
            DeleteContactCommand = new RelayCommand(DeleteContact, CanEditContact);
            SaveContactsCommand = new RelayCommand(SaveContacts);
            LoadContactsCommand = new RelayCommand(LoadContacts);

            SelectedContact.PropertyChanged += (_, __) => OnPropertyChanged(nameof(IsAddEditEnabled));
        }

        public ObservableCollection<Contact> Contacts { get; set; }

        public Contact SelectedContact
        {
            get => selectedContact;
            set
            {
                if (selectedContact != value)
                {
                    if (selectedContact != null)
                    {
                        selectedContact.PropertyChanged -= SelectedContact_PropertyChanged;
                    }

                    selectedContact = value;

                    if (selectedContact != null)
                    {
                        selectedContact.PropertyChanged += SelectedContact_PropertyChanged;
                    }

                    OnPropertyChanged();
                    (AddEditContactCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private void SelectedContact_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            (AddEditContactCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }


        public string AddEditButtonContent
        {
            get => addEditButtonContent;
            set
            {
                if (addEditButtonContent != value)
                {
                    addEditButtonContent = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsAddEditEnabled =>
                !string.IsNullOrWhiteSpace(SelectedContact.Name) &&
                !string.IsNullOrWhiteSpace(SelectedContact.Surname) &&
                !string.IsNullOrWhiteSpace(SelectedContact.Address) &&
                !string.IsNullOrWhiteSpace(SelectedContact.Phone) &&
                SelectedContact.Name.Length >= 3 &&
                SelectedContact.Surname.Length >= 3 &&
                SelectedContact.Address.Length >= 3 &&
                Regex.IsMatch(SelectedContact.Name, @"^[a-zA-Zа-яА-Я\s]+$") &&
                Regex.IsMatch(SelectedContact.Surname, @"^[a-zA-Zа-яА-Я\s]+$") &&
                Regex.IsMatch(SelectedContact.Phone, @"^\+?\d+$");


        public ICommand AddEditContactCommand { get; }
        public ICommand EditContactCommand { get; }
        public ICommand DeleteContactCommand { get; }
        public ICommand SaveContactsCommand { get; }
        public ICommand LoadContactsCommand { get; }

        private void AddOrEditContact()
        {
            if (!IsAddEditEnabled) return;

            if (AddEditButtonContent == "Изменить")
            {
                var existingContact = Contacts.FirstOrDefault(c => c == SelectedContact);
                if (existingContact != null)
                {
                    existingContact.Name = SelectedContact.Name;
                    existingContact.Surname = SelectedContact.Surname;
                    existingContact.Address = SelectedContact.Address;
                    existingContact.Phone = SelectedContact.Phone;

                    OnPropertyChanged(nameof(Contacts));
                }
                AddEditButtonContent = "Добавить";
            }
            else
            {
                Contacts.Add(new Contact
                {
                    Name = SelectedContact.Name,
                    Surname = SelectedContact.Surname,
                    Address = SelectedContact.Address,
                    Phone = SelectedContact.Phone
                });
            }
            SelectedContact = new Contact();
        }


        private bool CanAddOrEditContact() =>
                 IsAddEditEnabled;

        private void EditContact()
        {
            if (SelectedContact != null)
            {
                AddEditButtonContent = "Изменить";
            }
        }

        private void DeleteContact()
        {
            if (SelectedContact != null)
            {
                Contacts.Remove(SelectedContact);
                SelectedContact = new Contact();
                AddEditButtonContent = "Добавить";
            }
        }

        private bool CanEditContact() => SelectedContact != null && Contacts.Contains(SelectedContact);

        private void SaveContacts()
        {
            try
            {
                if (Contacts.Count == 0)
                {
                    MessageBox.Show("Список контактов пуст. Добавьте хотя бы один контакт перед сохранением.",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string json = JsonSerializer.Serialize(Contacts);
                File.WriteAllText("contacts.json", json);
                MessageBox.Show("Контакты успешно сохранены.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadContacts()
        {
            try
            {
                if (!File.Exists("contacts.json"))
                {
                    MessageBox.Show("Файл контактов не найден.", "Загрузка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string json = File.ReadAllText("contacts.json");
                var loadedContacts = JsonSerializer.Deserialize<ObservableCollection<Contact>>(json);

                if (loadedContacts == null || !loadedContacts.Any())
                {
                    MessageBox.Show("Файл контактов пуст. Добавьте хотя бы один контакт в файл.",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Contacts.Clear();
                foreach (var contact in loadedContacts)
                {
                    Contacts.Add(contact);
                }

                MessageBox.Show("Контакты успешно загружены.", "Загрузка", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (propertyName == nameof(SelectedContact))
            {
                (AddEditContactCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }
}
