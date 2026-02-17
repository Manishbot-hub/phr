using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace PharmacyManagementApp;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly DispatcherTimer _clockTimer;
    private string _inventoryFilter = string.Empty;
    private string _footerMessage = "Use keyboard shortcuts for fast operation.";
    private Medicine _currentMedicine = new();
    private Medicine? _selectedMedicine;

    public ObservableCollection<Medicine> Medicines { get; } = new();
    public ObservableCollection<Medicine> FilteredMedicines { get; } = new();
    public ObservableCollection<BillItem> BillItems { get; } = new();
    public ObservableCollection<Supplier> Suppliers { get; } = new();
    public ObservableCollection<Prescription> Prescriptions { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        SeedData();
        ApplyInventoryFilter();

        CommandBindings.Add(new CommandBinding(ApplicationCommands.New, (_, _) => NewItem()));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, (_, _) => SaveItem()));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, (_, _) => DeleteItem()));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, (_, _) => FocusSearch()));
        CommandBindings.Add(new CommandBinding(NavigationCommands.Refresh, (_, _) => RefreshDashboard()));

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => OnPropertyChanged(nameof(CurrentDateTime));
        _clockTimer.Start();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Medicine CurrentMedicine
    {
        get => _currentMedicine;
        set => SetField(ref _currentMedicine, value);
    }

    public Medicine? SelectedMedicine
    {
        get => _selectedMedicine;
        set
        {
            if (SetField(ref _selectedMedicine, value) && value is not null)
            {
                CurrentMedicine = value.Clone();
                FooterMessage = $"Editing {value.Name}";
            }
        }
    }

    public string InventoryFilter
    {
        get => _inventoryFilter;
        set
        {
            if (SetField(ref _inventoryFilter, value))
            {
                ApplyInventoryFilter();
            }
        }
    }

    public string FooterMessage
    {
        get => _footerMessage;
        set => SetField(ref _footerMessage, value);
    }

    public DateTime CurrentDateTime => DateTime.Now;

    public decimal TodaySales { get; private set; }
    public int LowStockCount => Medicines.Count(m => m.StockQuantity <= m.ReorderLevel);
    public int ExpiringSoonCount => Medicines.Count(m => m.ExpiryDate <= DateTime.Today.AddDays(30));

    public string SaleCustomerName { get; set; } = string.Empty;
    public Medicine? SelectedSaleMedicine { get; set; }
    public int SaleQuantity { get; set; } = 1;

    public decimal BillSubtotal => BillItems.Sum(b => b.LineTotal);
    public decimal BillTax => BillSubtotal * 0.05m;
    public decimal BillTotal => BillSubtotal + BillTax;

    public string NewSupplierName { get; set; } = string.Empty;
    public string NewSupplierPhone { get; set; } = string.Empty;
    public string NewSupplierEmail { get; set; } = string.Empty;

    public string NewPrescriptionPatient { get; set; } = string.Empty;
    public string NewPrescriptionDoctor { get; set; } = string.Empty;
    public Medicine? NewPrescriptionMedicine { get; set; }

    private void SeedData()
    {
        Medicines.Add(new Medicine("Paracetamol 500mg", "Analgesic", "B-1001", DateTime.Today.AddMonths(10), 120, 25, 3.50m));
        Medicines.Add(new Medicine("Amoxicillin 250mg", "Antibiotic", "B-2044", DateTime.Today.AddMonths(6), 60, 20, 8.20m));
        Medicines.Add(new Medicine("Cetirizine", "Allergy", "B-8840", DateTime.Today.AddMonths(4), 40, 15, 4.10m));
        Medicines.Add(new Medicine("Omeprazole", "Gastro", "B-6622", DateTime.Today.AddMonths(2), 18, 20, 6.90m));

        Suppliers.Add(new Supplier("HealthPlus Distributors", "+1-555-1200", "sales@healthplus.com"));
        Suppliers.Add(new Supplier("MediCore Supply", "+1-555-9088", "orders@medicore.com"));
    }

    private void NewItem()
    {
        CurrentMedicine = new Medicine();
        SelectedMedicine = null;
        FooterMessage = "Ready to add a new medicine.";
    }

    private void SaveItem()
    {
        if (string.IsNullOrWhiteSpace(CurrentMedicine.Name))
        {
            MessageBox.Show("Please enter a medicine name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var existing = Medicines.FirstOrDefault(m => m.BatchNumber.Equals(CurrentMedicine.BatchNumber, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            Medicines.Add(CurrentMedicine.Clone());
            FooterMessage = $"Added {CurrentMedicine.Name}";
        }
        else
        {
            existing.CopyFrom(CurrentMedicine);
            FooterMessage = $"Updated {CurrentMedicine.Name}";
        }

        ApplyInventoryFilter();
        RefreshDashboard();
    }

    private void DeleteItem()
    {
        if (SelectedMedicine is null)
        {
            FooterMessage = "Select an inventory row first.";
            return;
        }

        Medicines.Remove(SelectedMedicine);
        FooterMessage = $"Deleted {SelectedMedicine.Name}";
        NewItem();
        ApplyInventoryFilter();
        RefreshDashboard();
    }

    private void FocusSearch()
    {
        DrugNameTextBox.Focus();
        FooterMessage = "Cursor focused on inventory form.";
    }

    private void RefreshDashboard()
    {
        OnPropertyChanged(nameof(TodaySales));
        OnPropertyChanged(nameof(LowStockCount));
        OnPropertyChanged(nameof(ExpiringSoonCount));
        OnPropertyChanged(nameof(BillSubtotal));
        OnPropertyChanged(nameof(BillTax));
        OnPropertyChanged(nameof(BillTotal));
        FooterMessage = "Dashboard refreshed.";
    }

    private void ApplyInventoryFilter()
    {
        var term = InventoryFilter.Trim();
        var results = string.IsNullOrWhiteSpace(term)
            ? Medicines
            : new ObservableCollection<Medicine>(Medicines.Where(m =>
                m.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                m.Category.Contains(term, StringComparison.OrdinalIgnoreCase)));

        FilteredMedicines.Clear();
        foreach (var medicine in results)
        {
            FilteredMedicines.Add(medicine);
        }

        RefreshDashboard();
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void Shortcuts_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Ctrl+N: New item\nCtrl+S: Save item\nCtrl+Delete: Delete item\nCtrl+F: Focus form\nF5: Refresh", "Keyboard Shortcuts", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void AddToBill_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedSaleMedicine is null || SaleQuantity <= 0)
        {
            FooterMessage = "Pick medicine and valid quantity.";
            return;
        }

        if (SelectedSaleMedicine.StockQuantity < SaleQuantity)
        {
            FooterMessage = "Insufficient stock.";
            return;
        }

        BillItems.Add(new BillItem(SelectedSaleMedicine.Name, SaleQuantity, SelectedSaleMedicine.UnitPrice));
        SelectedSaleMedicine.StockQuantity -= SaleQuantity;
        SaleQuantity = 1;
        OnPropertyChanged(nameof(SaleQuantity));
        RefreshDashboard();
    }

    private void CompleteSale_Click(object sender, RoutedEventArgs e)
    {
        if (BillItems.Count == 0)
        {
            FooterMessage = "Nothing to bill.";
            return;
        }

        TodaySales += BillTotal;
        FooterMessage = $"Sale completed for {SaleCustomerName}. Total {BillTotal:C}";
        BillItems.Clear();
        OnPropertyChanged(nameof(TodaySales));
        RefreshDashboard();
    }

    private void ClearBill_Click(object sender, RoutedEventArgs e)
    {
        BillItems.Clear();
        FooterMessage = "Current bill cleared.";
        RefreshDashboard();
    }

    private void AddSupplier_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewSupplierName))
        {
            FooterMessage = "Supplier name required.";
            return;
        }

        Suppliers.Add(new Supplier(NewSupplierName, NewSupplierPhone, NewSupplierEmail));
        NewSupplierName = NewSupplierPhone = NewSupplierEmail = string.Empty;
        OnPropertyChanged(nameof(NewSupplierName));
        OnPropertyChanged(nameof(NewSupplierPhone));
        OnPropertyChanged(nameof(NewSupplierEmail));
        FooterMessage = "Supplier added.";
    }

    private void AddPrescription_Click(object sender, RoutedEventArgs e)
    {
        if (NewPrescriptionMedicine is null || string.IsNullOrWhiteSpace(NewPrescriptionPatient))
        {
            FooterMessage = "Prescription requires patient and medicine.";
            return;
        }

        Prescriptions.Add(new Prescription(NewPrescriptionPatient, NewPrescriptionDoctor, NewPrescriptionMedicine.Name, DateTime.Now));
        NewPrescriptionPatient = NewPrescriptionDoctor = string.Empty;
        NewPrescriptionMedicine = null;
        OnPropertyChanged(nameof(NewPrescriptionPatient));
        OnPropertyChanged(nameof(NewPrescriptionDoctor));
        OnPropertyChanged(nameof(NewPrescriptionMedicine));
        FooterMessage = "Prescription queued.";
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class Medicine
{
    public Medicine()
    {
        ExpiryDate = DateTime.Today.AddMonths(12);
        BatchNumber = "AUTO";
    }

    public Medicine(string name, string category, string batchNumber, DateTime expiryDate, int stockQuantity, int reorderLevel, decimal unitPrice)
    {
        Name = name;
        Category = category;
        BatchNumber = batchNumber;
        ExpiryDate = expiryDate;
        StockQuantity = stockQuantity;
        ReorderLevel = reorderLevel;
        UnitPrice = unitPrice;
    }

    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int StockQuantity { get; set; }
    public int ReorderLevel { get; set; }
    public decimal UnitPrice { get; set; }

    public Medicine Clone() => new(Name, Category, BatchNumber, ExpiryDate, StockQuantity, ReorderLevel, UnitPrice);

    public void CopyFrom(Medicine other)
    {
        Name = other.Name;
        Category = other.Category;
        BatchNumber = other.BatchNumber;
        ExpiryDate = other.ExpiryDate;
        StockQuantity = other.StockQuantity;
        ReorderLevel = other.ReorderLevel;
        UnitPrice = other.UnitPrice;
    }
}

public record BillItem(string MedicineName, int Quantity, decimal UnitPrice)
{
    public decimal LineTotal => Quantity * UnitPrice;
}

public record Supplier(string Name, string Phone, string Email);

public record Prescription(string PatientName, string DoctorName, string MedicineName, DateTime CreatedOn);
