package com.app.nutriredapp.ui.donation

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.app.nutriredapp.data.model.Donation
import com.app.nutriredapp.data.model.DonationItem
import com.app.nutriredapp.data.model.Donor
import com.app.nutriredapp.data.model.DonorType
import com.app.nutriredapp.data.model.Product
import com.app.nutriredapp.data.repository.DonationRepository
import com.app.nutriredapp.data.repository.DonorRepository
import com.app.nutriredapp.data.repository.ProductRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import java.text.SimpleDateFormat
import java.util.*

class DonationViewModel(
    private val donorRepository: DonorRepository = DonorRepository(),
    private val productRepository: ProductRepository = ProductRepository(),
    private val donationRepository: DonationRepository = DonationRepository()
) : ViewModel() {

    private val _uiState = MutableStateFlow(DonationUiState())
    val uiState: StateFlow<DonationUiState> = _uiState.asStateFlow()

    private val dateFormatter = SimpleDateFormat("dd/MM/yyyy", Locale.getDefault()).apply {
        isLenient = false
    }

    init {
        loadCatalog()
        loadCategories()
    }

    private fun loadCatalog() {
        viewModelScope.launch {
            val products = productRepository.getAllProducts()
            _uiState.update { it.copy(catalogProducts = products) }
        }
    }

    private fun loadCategories() {
        viewModelScope.launch {
            val cats = productRepository.getAllCategories()
            val defaultCat = cats.find { it.name.contains("otro", ignoreCase = true) || it.name.contains("vario", ignoreCase = true) } ?: cats.firstOrNull()
            _uiState.update {
                it.copy(
                    categories = cats,
                    selectedCategoryId = defaultCat?.id,
                    selectedCategoryName = defaultCat?.name ?: "Otros Alimentos / Varios"
                )
            }
        }
    }

    fun onCategorySelected(id: Int?, name: String) {
        val resolvedId = id ?: _uiState.value.categories.find { it.name.trim().equals(name.trim(), ignoreCase = true) }?.id
        _uiState.update {
            it.copy(
                selectedCategoryId = resolvedId,
                selectedCategoryName = name
            )
        }
    }

    // --- ACCIONES PASO 1: DONANTE ---

    fun onDonorTypeChanged(type: DonorType) {
        _uiState.update {
            it.copy(
                donorType = type,
                donorError = null,
                // Si es anónimo, limpiamos los campos y errores
                documentNumber = if (type == DonorType.ANONYMOUS) "" else it.documentNumber,
                donorName = if (type == DonorType.ANONYMOUS) "Anónimo" else it.donorName
            )
        }
        if (type == DonorType.ANONYMOUS) {
            // Avanzar automáticamente o dejar listo para continuar con 1 toque
            _uiState.update { it.copy(isDonorExisting = true) }
        }
    }

    fun onDocumentNumberChanged(doc: String) {
        _uiState.update { it.copy(documentNumber = doc, donorError = null) }
        if (doc.length >= 7) {
            checkExistingDonor(doc)
        }
    }

    private fun checkExistingDonor(doc: String) {
        viewModelScope.launch {
            val existing = donorRepository.findDonorByDocument(doc)
            if (existing != null) {
                _uiState.update {
                    it.copy(
                        donorName = existing.name ?: "",
                        donorPhone = existing.phone ?: "",
                        donorEmail = existing.email ?: "",
                        donorType = existing.type,
                        isDonorExisting = true
                    )
                }
            } else {
                _uiState.update { it.copy(isDonorExisting = false) }
            }
        }
    }

    fun onDonorNameChanged(name: String) {
        _uiState.update { it.copy(donorName = name, donorError = null) }
    }

    fun onDonorPhoneChanged(phone: String) {
        _uiState.update { it.copy(donorPhone = phone) }
    }

    fun onDonorEmailChanged(email: String) {
        _uiState.update { it.copy(donorEmail = email) }
    }

    fun validateAndProceedToItems(): Boolean {
        val state = _uiState.value
        if (state.donorType == DonorType.ANONYMOUS) {
            _uiState.update { it.copy(currentStep = DonationStep.ITEMS, donorError = null) }
            return true
        }

        if (state.documentNumber.isBlank()) {
            _uiState.update { it.copy(donorError = "El número de documento (DNI/CUIT) es obligatorio.") }
            return false
        }

        if (state.donorName.isBlank()) {
            _uiState.update { it.copy(donorError = "Debe ingresar el Nombre o Razón Social del donante.") }
            return false
        }

        _uiState.update { it.copy(currentStep = DonationStep.ITEMS, donorError = null) }
        return true
    }

    // --- ACCIONES PASO 2: ALIMENTOS Y LOTES FEFO ---

    fun onBarcodeScanned(barcode: String) {
        _uiState.update { it.copy(currentBarcode = barcode, isCameraScanning = false) }
        searchProductInCatalog(barcode)
    }

    fun onBarcodeChanged(barcode: String) {
        _uiState.update { it.copy(currentBarcode = barcode, itemErrorMessage = null) }
        if (barcode.length >= 8) {
            searchProductInCatalog(barcode)
        }
    }

    fun searchProductInCatalog(barcode: String) {
        viewModelScope.launch {
            val product = productRepository.findProductByBarcode(barcode)
            if (product != null) {
                _uiState.update {
                    it.copy(
                        currentProductName = product.name,
                        currentUnitOfMeasure = product.unitOfMeasure,
                        selectedCategoryId = product.categoryId,
                        selectedCategoryName = product.category,
                        isProductFromCatalog = true,
                        itemErrorMessage = null
                    )
                }
            } else {
                _uiState.update {
                    val defCat = it.categories.find { c -> c.name.contains("otro", ignoreCase = true) || c.name.contains("vario", ignoreCase = true) } ?: it.categories.firstOrNull()
                    it.copy(
                        isProductFromCatalog = false,
                        selectedCategoryId = defCat?.id ?: it.selectedCategoryId,
                        selectedCategoryName = defCat?.name ?: it.selectedCategoryName
                    )
                }
            }
        }
    }

    fun selectProductFromCatalog(product: Product) {
        _uiState.update {
            it.copy(
                currentBarcode = product.barcode,
                currentProductName = product.name,
                currentUnitOfMeasure = product.unitOfMeasure,
                selectedCategoryId = product.categoryId,
                selectedCategoryName = product.category,
                isProductFromCatalog = true,
                itemErrorMessage = null
            )
        }
    }

    fun onProductNameChanged(name: String) {
        _uiState.update { it.copy(currentProductName = name, itemErrorMessage = null) }
    }

    fun onQuantityChanged(qty: String) {
        _uiState.update { it.copy(currentQuantity = qty, itemErrorMessage = null) }
    }

    fun onUnitOfMeasureChanged(unit: String) {
        _uiState.update { it.copy(currentUnitOfMeasure = unit) }
    }

    val availableMonths = listOf(
        "Enero", "Febrero", "Marzo", "Abril",
        "Mayo", "Junio", "Julio", "Agosto",
        "Septiembre", "Octubre", "Noviembre", "Diciembre"
    )

    fun getAvailableYears(): List<String> {
        val currentYear = Calendar.getInstance().get(Calendar.YEAR)
        return (currentYear..(currentYear + 10)).map { it.toString() }
    }

    fun onExpirationMonthChanged(monthText: String) {
        val monthIndex = availableMonths.indexOfFirst { it.equals(monthText.trim(), ignoreCase = true) }
        val monthCode = if (monthIndex != -1) {
            String.format("%02d", monthIndex + 1)
        } else {
            monthText.filter { it.isDigit() }.padStart(2, '0').take(2)
        }
        val currentYear = _uiState.value.currentExpirationYear
        val combined = if (currentYear.isNotEmpty()) "$monthCode/$currentYear" else monthCode
        _uiState.update {
            it.copy(
                currentExpirationMonth = monthCode,
                currentExpirationDate = combined,
                itemErrorMessage = null
            )
        }
    }

    fun onExpirationYearChanged(yearText: String) {
        val currentMonth = _uiState.value.currentExpirationMonth
        val combined = if (currentMonth.isNotEmpty()) "$currentMonth/$yearText" else yearText
        _uiState.update {
            it.copy(
                currentExpirationYear = yearText,
                currentExpirationDate = combined,
                itemErrorMessage = null
            )
        }
    }

    fun onExpirationDateChanged(date: String) {
        val prev = _uiState.value.currentExpirationDate
        val isDeleting = date.length < prev.length

        val cleanDigits = if (isDeleting && prev.endsWith("/") && !date.endsWith("/")) {
            date.filter { it.isDigit() }.dropLast(1)
        } else {
            date.filter { it.isDigit() }.take(8)
        }

        val formatted = buildString {
            for (i in cleanDigits.indices) {
                append(cleanDigits[i])
                if (i == 1 || i == 3) {
                    append('/')
                }
            }
        }

        val parts = formatted.split("/")
        val month = parts.getOrNull(0) ?: ""
        val year = parts.getOrNull(1) ?: ""

        _uiState.update {
            it.copy(
                currentExpirationDate = formatted,
                currentExpirationMonth = month,
                currentExpirationYear = year,
                itemErrorMessage = null
            )
        }
    }

    fun onBatchNumberChanged(batch: String) {
        _uiState.update { it.copy(currentBatchNumber = batch) }
    }

    fun setCameraScanning(active: Boolean) {
        _uiState.update { it.copy(isCameraScanning = active) }
    }

    fun addCurrentItemToList(): Boolean {
        val state = _uiState.value

        // 1. Validar nombre y código de barras
        if (state.currentProductName.isBlank()) {
            _uiState.update { it.copy(itemErrorMessage = "El nombre del alimento es obligatorio.") }
            return false
        }
        val barcode = if (state.currentBarcode.isBlank()) {
            "MAN-${System.currentTimeMillis() % 1000000}"
        } else {
            state.currentBarcode
        }

        // 2. Validar cantidad mayor a cero
        val qty = state.currentQuantity.toDoubleOrNull()
        if (qty == null || qty <= 0.0) {
            _uiState.update { it.copy(itemErrorMessage = "Ingrese una cantidad válida y mayor a cero.") }
            return false
        }

        // 3. Validar fecha de vencimiento (Mes / Año)
        val expDateStr = state.currentExpirationDate.trim()
        if (expDateStr.isBlank() || !expDateStr.contains("/")) {
            _uiState.update { it.copy(itemErrorMessage = "Debe seleccionar el mes y año de vencimiento.") }
            return false
        }

        val currentCalendar = Calendar.getInstance()
        val currentYear = currentCalendar.get(Calendar.YEAR)
        val currentMonth = currentCalendar.get(Calendar.MONTH) + 1 // 1..12

        val (expMonth, expYear) = if (expDateStr.contains("/")) {
            val parts = expDateStr.split("/")
            if (parts.size == 2) {
                // Formato MM/AAAA
                Pair(parts[0].toIntOrNull(), parts[1].toIntOrNull())
            } else if (parts.size == 3) {
                // Formato DD/MM/AAAA
                Pair(parts[1].toIntOrNull(), parts[2].toIntOrNull())
            } else {
                Pair(null, null)
            }
        } else {
            Pair(null, null)
        }

        if (expMonth == null || expYear == null || expMonth !in 1..12) {
            _uiState.update { it.copy(itemErrorMessage = "Mes o año de vencimiento inválido.") }
            return false
        }

        if (expYear < currentYear || (expYear == currentYear && expMonth <= currentMonth)) {
            _uiState.update {
                it.copy(itemErrorMessage = "No se pueden recibir alimentos vencidos o que vencen en el mes actual.")
            }
            return false
        }

        // 4. Si el producto no estaba en catálogo, registrarlo en el sistema
        if (!state.isProductFromCatalog) {
            viewModelScope.launch {
                productRepository.addProduct(
                    Product(
                        barcode = barcode,
                        name = state.currentProductName.trim(),
                        unitOfMeasure = state.currentUnitOfMeasure,
                        category = state.selectedCategoryName,
                        categoryId = state.selectedCategoryId
                    )
                )
                loadCatalog()
            }
        }

        // 5. Generar lote si no se indicó
        val batch = if (state.currentBatchNumber.isBlank()) {
            "LOTE-${SimpleDateFormat("yyyyMMdd", Locale.getDefault()).format(Date())}-${(100..999).random()}"
        } else {
            state.currentBatchNumber.trim()
        }

        val newItem = DonationItem(
            id = "ITEM-${UUID.randomUUID().toString().take(8)}",
            productBarcode = barcode,
            productName = state.currentProductName.trim(),
            quantity = qty,
            unitOfMeasure = state.currentUnitOfMeasure,
            expirationDate = expDateStr,
            batchNumber = batch,
            categoryId = state.selectedCategoryId,
            categoryName = state.selectedCategoryName
        )

        val defaultCat = state.categories.find { c -> c.name.contains("otro", ignoreCase = true) || c.name.contains("vario", ignoreCase = true) } ?: state.categories.firstOrNull()

        _uiState.update {
            it.copy(
                items = it.items + newItem,
                // Limpiar formulario de ítem para el siguiente
                currentBarcode = "",
                currentProductName = "",
                currentQuantity = "1",
                currentExpirationMonth = "",
                currentExpirationYear = "",
                currentExpirationDate = "",
                currentBatchNumber = "",
                isProductFromCatalog = false,
                selectedCategoryId = defaultCat?.id,
                selectedCategoryName = defaultCat?.name ?: "Otros Alimentos / Varios",
                itemErrorMessage = null
            )
        }
        return true
    }

    fun removeItem(item: DonationItem) {
        _uiState.update {
            it.copy(items = it.items.filterNot { i -> i.id == item.id })
        }
    }

    fun proceedToSummary(): Boolean {
        val state = _uiState.value
        if (state.items.isEmpty()) {
            _uiState.update { it.copy(itemErrorMessage = "Debe cargar al menos un alimento antes de continuar.") }
            return false
        }
        _uiState.update { it.copy(currentStep = DonationStep.SUMMARY, itemErrorMessage = null) }
        return true
    }

    fun goToStep(step: DonationStep) {
        _uiState.update { it.copy(currentStep = step, itemErrorMessage = null, donorError = null) }
    }

    // --- ACCIONES PASO 3: CONFIRMACIÓN Y COMPROBANTE ---

    fun confirmDonation() {
        val state = _uiState.value
        if (state.items.isEmpty()) {
            _uiState.update { it.copy(globalError = "No hay productos en la donación.") }
            return
        }

        viewModelScope.launch {
            _uiState.update { it.copy(isSubmitting = true, globalError = null) }

            try {
                // Registrar o vincular donante
                val donorToSave = if (state.donorType == DonorType.ANONYMOUS) {
                    Donor(
                        id = "ANON-${System.currentTimeMillis() % 10000}",
                        type = DonorType.ANONYMOUS,
                        name = "Donante Anónimo"
                    )
                } else {
                    val donorObj = Donor(
                        type = state.donorType,
                        documentNumber = state.documentNumber.trim(),
                        name = state.donorName.trim(),
                        phone = state.donorPhone.trim().ifEmpty { null },
                        email = state.donorEmail.trim().ifEmpty { null }
                    )
                    donorRepository.saveDonor(donorObj)
                }

                val nowFormatted = SimpleDateFormat("dd/MM/yyyy HH:mm", Locale.getDefault()).format(Date())

                val firstItem = state.items.firstOrNull()
                val newDonation = Donation(
                    donationCode = "", // Se genera en el repository
                    barcode = firstItem?.productBarcode ?: "",
                    name = firstItem?.productName ?: "Donación de alimentos",
                    quantity = state.items.sumOf { it.quantity }.toInt(),
                    donorId = donorToSave.id,
                    donor = donorToSave,
                    volunteerId = "VOL-101",
                    volunteerName = "Voluntario Receptor",
                    timestamp = nowFormatted,
                    items = state.items
                )

                val saved = donationRepository.saveDonation(newDonation)

                _uiState.update {
                    it.copy(
                        isSubmitting = false,
                        completedDonation = saved,
                        currentStep = DonationStep.RECEIPT
                    )
                }
            } catch (e: Exception) {
                _uiState.update {
                    it.copy(
                        isSubmitting = false,
                        globalError = "Error al registrar la donación: ${e.message}"
                    )
                }
            }
        }
    }

    fun resetForNewDonation() {
        _uiState.value = DonationUiState(catalogProducts = _uiState.value.catalogProducts)
    }
}
