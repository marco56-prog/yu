using AccountingSystem.Business;
using AccountingSystem.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace AccountingSystem.WPF.ViewModels
{
    public class ProductViewModel : BaseViewModel
    {
        private readonly IProductService _productService;
        private ObservableCollection<Product> _products;
        private ICollectionView _productsView;
        private Product _selectedProduct;
        private string _searchText;

        public ObservableCollection<Product> Products
        {
            get => _products;
            set
            {
                _products = value;
                OnPropertyChanged(nameof(Products));
            }
        }

        public ICollectionView ProductsView
        {
            get => _productsView;
            set
            {
                _productsView = value;
                OnPropertyChanged(nameof(ProductsView));
            }
        }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged(nameof(SelectedProduct));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                ProductsView?.Refresh();
            }
        }

        public ICommand AddProductCommand { get; }
        public ICommand EditProductCommand { get; }
        public ICommand DeleteProductCommand { get; }
        public ICommand RefreshCommand { get; }

        public ProductViewModel(IProductService productService)
        {
            _productService = productService;
            _products = new ObservableCollection<Product>();
            _productsView = CollectionViewSource.GetDefaultView(_products);
            _productsView.Filter = FilterProducts;

            AddProductCommand = new RelayCommand(async () => await AddProduct());
            EditProductCommand = new RelayCommand(async () => await EditProduct(), CanExecuteEditDelete);
            DeleteProductCommand = new RelayCommand(async () => await DeleteProduct(), CanExecuteEditDelete);
            RefreshCommand = new RelayCommand(async () => await LoadProducts());

            _ = LoadProducts();
        }

        private async Task LoadProducts()
        {
            var productsList = await _productService.GetAllProductsAsync();
            Products = new ObservableCollection<Product>(productsList);
            ProductsView = CollectionViewSource.GetDefaultView(Products);
            ProductsView.Filter = FilterProducts;
            OnPropertyChanged(nameof(Products));
            OnPropertyChanged(nameof(ProductsView));
        }

        private bool FilterProducts(object item)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return true;
            }

            if (item is Product product)
            {
                return product.ProductName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                       product.ProductCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private async Task AddProduct()
        {
            var newProduct = new Product { ProductName = "New Product", ProductCode = "PROD-NEW" }; // Default values
            var dialog = new Views.ProductEntryDialog(newProduct)
            {
                Title = "Add New Product"
            };

            if (dialog.ShowDialog() == true)
            {
                await _productService.AddProductAsync(newProduct);
                await LoadProducts();
            }
        }

        private async Task EditProduct()
        {
            if (SelectedProduct == null) return;

            var productToEdit = (Product)SelectedProduct.Clone();
            var dialog = new Views.ProductEntryDialog(productToEdit)
            {
                Title = "Edit Product"
            };

            if (dialog.ShowDialog() == true)
            {
                await _productService.UpdateProductAsync(productToEdit);
                await LoadProducts();
            }
        }

        private async Task DeleteProduct()
        {
            if (SelectedProduct != null)
            {
                await _productService.DeleteProductAsync(SelectedProduct.ProductId);
                await LoadProducts();
            }
        }

        private bool CanExecuteEditDelete()
        {
            return SelectedProduct != null;
        }
    }
}