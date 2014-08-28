using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using Pronunciation.Core.Database;
using System.Collections.ObjectModel;
using Pronunciation.Trainer.Utility;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for EditCategories.xaml
    /// </summary>
    public partial class EditCategories : Window
    {
        private Entities _dbContext;
        private Lazy<ObservableCollection<DictionaryCategory>> _categories;

        private const string DefaultDisplayName = "New category";

        public EditCategories()
        {
            InitializeComponent();
        }

        public ObservableCollection<DictionaryCategory> Categories
        {
            get { return _categories.Value; }
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            _dbContext = new Entities();
            _categories = new Lazy<ObservableCollection<DictionaryCategory>>(() =>
            {
                // We can't use shorter syntax here because it's DB query
                _dbContext.DictionaryCategories.Where(x => (x.IsSystemCategory == null || x.IsSystemCategory == false)).ToList();
                return _dbContext.DictionaryCategories.Local;
            });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var newCategory = _dbContext.DictionaryCategories.Create();
            newCategory.CategoryId = Guid.NewGuid();
            newCategory.DisplayName = DefaultDisplayName;
            _dbContext.DictionaryCategories.Add(newCategory);
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            // We must read them all before enumerating
            var selectedCategories = categoriesDataGrid.SelectedCells.Where(x => (x.Item is DictionaryCategory))
                .Select(x => x.Item as DictionaryCategory)
                .Distinct(new DictionaryCategoryComparer())
                .ToArray();
            if (selectedCategories.Length <= 0)
                return;

            foreach (var category in selectedCategories)
            {
                _dbContext.DictionaryCategories.Remove(category);
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (_dbContext.HasChanges())
            {
                _dbContext.SaveChanges();
            }

            if (ControlsHelper.IsModalWindow)
            {
                this.DialogResult = true;
            }
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (ControlsHelper.IsExplicitCloseRequired(btnCancel))
            {
                this.Close();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_dbContext.HasChanges())
            {
                if(!MessageHelper.ShowConfirmation(
                    "You have some pending changes. Are you sure you want to discard them?",
                    "Confirm discarding changes"))
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        private class DictionaryCategoryComparer : IEqualityComparer<DictionaryCategory>
        {
            public bool Equals(DictionaryCategory x, DictionaryCategory y)
            {
                if (x != null && y != null)
                {
                    return x.CategoryId == y.CategoryId;
                }
                else
                {
                    return x == null && y == null;
                }
            }

            public int GetHashCode(DictionaryCategory obj)
            {
                return obj == null ? 0 : obj.CategoryId.GetHashCode();
            }
        }
    }
}
