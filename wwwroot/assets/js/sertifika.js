document.addEventListener('DOMContentLoaded', function () {
    // Modal elementi al
    var modal = document.getElementById("myModal");

    // Modal içindeki img elementi al
    var modalImg = document.getElementById("img01");
    var captionText = document.getElementById("caption");

    // Header elementini seç
    var header = document.querySelector("header");
    var gotoTop = document.getElementById("gotoTop");

    // Tüm image-link sınıflı öğeleri al
    var images = document.getElementsByClassName("image-link");

    // Her bir image-link için tıklama olayı ekle
    for (var i = 0; i < images.length; i++) {
        images[i].onclick = function (event) {
            event.preventDefault();
            modal.style.display = "block";
            modalImg.src = this.href;
            captionText.innerHTML = this.querySelector("img").alt;
            document.body.style.overflow = 'hidden'; // Modal açıldığında sayfa kaydırmasını engelle
            
            // Header'ı gizle
            if (header) {
                header.style.display = 'none';
            }
            
            // GoToTop butonunu gizle
            if (gotoTop) {
                gotoTop.style.display = 'none';
            }
        }
    }

    // Kapatma düğmesi
    var closeButton = document.querySelector(".close, .close-btn");

    // Kapatma düğmesine tıklandığında modalı kapat
    closeButton.onclick = function () {
        modal.style.display = "none";
        document.body.style.overflow = 'auto'; // Modal kapandığında sayfa kaydırmasını geri getir
        
        // Header'ı göster
        if (header) {
            header.style.display = '';
        }
        
        // GoToTop butonunu göster
        if (gotoTop) {
            gotoTop.style.display = '';
        }
    };

    // Modal dışına tıklanınca modalı kapat
    window.onclick = function (event) {
        if (event.target == modal) {
            modal.style.display = "none";
            document.body.style.overflow = 'auto'; // Modal kapandığında sayfa kaydırmasını geri getir
            
            // Header'ı göster
            if (header) {
                header.style.display = '';
            }
            
            // GoToTop butonunu göster
            if (gotoTop) {
                gotoTop.style.display = '';
            }
        }
    };
});

