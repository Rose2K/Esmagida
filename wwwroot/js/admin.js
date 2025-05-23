// Admin Panel Javascript

document.addEventListener('DOMContentLoaded', function() {
    // Initialize popovers
    initializePopovers();
    
    // Initialize product modal
    initializeProductModal();
    
    // Initialize search functionality
    initializeSearch();
    
    // Initialize delete confirmations
    initializeDeleteConfirmations();
    
    // Initialize chart if on reports page
    if (window.location.href.includes('/Admin/Reports')) {
        initializeCharts();
    }
});

/**
 * Initialize popovers for tooltips
 */
function initializePopovers() {
    const popoverTriggers = document.querySelectorAll('[data-toggle="popover"]');
    
    popoverTriggers.forEach(trigger => {
        trigger.addEventListener('mouseenter', function(e) {
            const target = document.querySelector(this.dataset.target);
            if (target) {
                target.classList.remove('hidden');
            }
        });
        
        trigger.addEventListener('mouseleave', function(e) {
            const target = document.querySelector(this.dataset.target);
            if (target) {
                target.classList.add('hidden');
            }
        });
    });
}

/**
 * Initialize product modal functionality
 */
function initializeProductModal() {
    const newProductBtn = document.getElementById('newProductBtn');
    const productModal = document.getElementById('productModal');
    const cancelProductBtn = document.getElementById('cancelProductBtn');
    const saveProductBtn = document.getElementById('saveProductBtn');
    const productForm = document.getElementById('productForm');
    
    if (newProductBtn && productModal) {
        newProductBtn.addEventListener('click', function() {
            productModal.classList.remove('hidden');
        });
        
        if (cancelProductBtn) {
            cancelProductBtn.addEventListener('click', function() {
                productModal.classList.add('hidden');
                // Reset form
                if (productForm) {
                    productForm.reset();
                }
            });
        }
        
        if (saveProductBtn && productForm) {
            saveProductBtn.addEventListener('click', function() {
                // Get form data
                const productName = document.getElementById('productName').value;
                const productCategory = document.getElementById('productCategory').value;
                const productStock = document.getElementById('productStock').value;
                const productPrice = document.getElementById('productPrice').value;
                
                // Validate form data
                if (!productName || !productCategory || !productStock || !productPrice) {
                    showAlert('Lütfen tüm alanları doldurunuz', 'error');
                    return;
                }
                
                // Submit form data via AJAX
                const productData = {
                    name: productName,
                    category: productCategory,
                    stock: parseInt(productStock),
                    price: parseFloat(productPrice)
                };
                
                fetch('/Admin/AddProduct', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    },
                    body: JSON.stringify(productData)
                })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        showAlert('Ürün başarıyla eklendi', 'success');
                        productModal.classList.add('hidden');
                        // Reload page after short delay
                        setTimeout(() => {
                            window.location.reload();
                        }, 1000);
                    } else {
                        showAlert('Ürün eklenirken bir hata oluştu', 'error');
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                    showAlert('İşlem sırasında bir hata oluştu', 'error');
                });
            });
        }
    }
}

/**
 * Initialize search functionality
 */
function initializeSearch() {
    const searchInput = document.querySelector('input[placeholder="Ürün Ara..."]');
    const searchInputCustomer = document.querySelector('input[placeholder="Müşteri Ara..."]');
    
    if (searchInput) {
        searchInput.addEventListener('keyup', function(e) {
            const searchTerm = e.target.value.toLowerCase();
            const table = document.querySelector('table');
            
            if (table) {
                const rows = table.querySelectorAll('tbody tr');
                
                rows.forEach(row => {
                    const text = row.textContent.toLowerCase();
                    if (text.includes(searchTerm)) {
                        row.style.display = '';
                    } else {
                        row.style.display = 'none';
                    }
                });
            }
        });
    }
    
    if (searchInputCustomer) {
        searchInputCustomer.addEventListener('keyup', function(e) {
            const searchTerm = e.target.value.toLowerCase();
            const table = document.querySelector('table');
            
            if (table) {
                const rows = table.querySelectorAll('tbody tr');
                
                rows.forEach(row => {
                    const text = row.textContent.toLowerCase();
                    if (text.includes(searchTerm)) {
                        row.style.display = '';
                    } else {
                        row.style.display = 'none';
                    }
                });
            }
        });
    }
}

/**
 * Initialize delete confirmations
 */
function initializeDeleteConfirmations() {
    const deleteButtons = document.querySelectorAll('.text-red-600');
    
    deleteButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            const confirmDelete = confirm('Bu öğeyi silmek istediğinizden emin misiniz?');
            
            if (confirmDelete) {
                const row = button.closest('tr');
                const id = row.querySelector('td:first-child').textContent.replace('#', '');
                
                // Determine if we're deleting a product or customer
                let endpoint = '/Admin/DeleteProduct';
                if (window.location.href.includes('/Admin/Customers')) {
                    endpoint = '/Admin/DeleteCustomer';
                }
                
                fetch(`${endpoint}?id=${id}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    }
                })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        showAlert('Öğe başarıyla silindi', 'success');
                        row.remove();
                    } else {
                        showAlert('Öğe silinirken bir hata oluştu', 'error');
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                    showAlert('İşlem sırasında bir hata oluştu', 'error');
                });
            }
        });
    });
}

/**
 * Initialize charts on reports page
 */
function initializeCharts() {
    // Only run if Chart.js is loaded
    if (typeof Chart === 'undefined') return;
    
    // Sample data
    const monthlyData = {
        labels: ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran'],
        datasets: [{
            label: 'Aylık Satış',
            data: [25450, 33240, 41800, 51200, 62700, 78450],
            backgroundColor: 'rgba(59, 130, 246, 0.2)',
            borderColor: 'rgba(59, 130, 246, 1)',
            borderWidth: 1
        }]
    };
    
    const categoryData = {
        labels: ['Elektronik', 'Giyim', 'Gıda', 'Diğer'],
        datasets: [{
            label: 'Kategori Dağılımı',
            data: [35, 25, 20, 20],
            backgroundColor: [
                'rgba(59, 130, 246, 0.6)',
                'rgba(34, 197, 94, 0.6)',
                'rgba(139, 92, 246, 0.6)',
                'rgba(250, 204, 21, 0.6)'
            ],
            borderColor: [
                'rgba(59, 130, 246, 1)',
                'rgba(34, 197, 94, 1)',
                'rgba(139, 92, 246, 1)',
                'rgba(250, 204, 21, 1)'
            ],
            borderWidth: 1
        }]
    };
    
    // Create sales chart
    const salesCanvas = document.getElementById('salesChart');
    if (salesCanvas) {
        const salesChart = new Chart(salesCanvas, {
            type: 'bar',
            data: monthlyData,
            options: {
                scales: {
                    y: {
                        beginAtZero: true
                    }
                },
                responsive: true,
                plugins: {
                    legend: {
                        position: 'top',
                    },
                    title: {
                        display: true,
                        text: 'Aylık Satış Grafiği'
                    }
                }
            }
        });
    }
    
    // Create category chart
    const categoryCanvas = document.getElementById('categoryChart');
    if (categoryCanvas) {
        const categoryChart = new Chart(categoryCanvas, {
            type: 'pie',
            data: categoryData,
            options: {
                responsive: true,
                plugins: {
                    legend: {
                        position: 'top',
                    },
                    title: {
                        display: true,
                        text: 'Kategori Dağılımı'
                    }
                }
            }
        });
    }
}

/**
 * Display an alert message
 * @param {string} message - The message to display
 * @param {string} type - The type of alert (success, error, info, warning)
 */
function showAlert(message, type = 'info') {
    // Check if an alert container exists
    let alertContainer = document.getElementById('alertContainer');
    
    // Create one if it doesn't exist
    if (!alertContainer) {
        alertContainer = document.createElement('div');
        alertContainer.id = 'alertContainer';
        alertContainer.style.position = 'fixed';
        alertContainer.style.top = '1rem';
        alertContainer.style.right = '1rem';
        alertContainer.style.zIndex = '9999';
        document.body.appendChild(alertContainer);
    }
    
    // Create the alert element
    const alert = document.createElement('div');
    alert.className = 'p-4 mb-4 rounded-md shadow-md transition-all transform translate-x-full';
    alert.style.maxWidth = '300px';
    
    // Set the appropriate styles based on type
    switch (type) {
        case 'success':
            alert.className += ' bg-green-100 border-l-4 border-green-500 text-green-700';
            break;
        case 'error':
            alert.className += ' bg-red-100 border-l-4 border-red-500 text-red-700';
            break;
        case 'warning':
            alert.className += ' bg-yellow-100 border-l-4 border-yellow-500 text-yellow-700';
            break;
        default:
            alert.className += ' bg-blue-100 border-l-4 border-blue-500 text-blue-700';
    }
    
    // Set the content
    alert.innerHTML = `
        <div class="flex justify-between items-center">
            <p>${message}</p>
            <button class="ml-4" onclick="this.parentElement.parentElement.remove()">
                <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
            </button>
        </div>
    `;
    
    // Add the alert to the container
    alertContainer.appendChild(alert);
    
    // Animate the alert
    setTimeout(() => {
        alert.style.transform = 'translateX(0)';
    }, 100);
    
    // Automatically remove the alert after 5 seconds
    setTimeout(() => {
        alert.style.opacity = '0';
        setTimeout(() => {
            alert.remove();
        }, 300);
    }, 5000);
} 