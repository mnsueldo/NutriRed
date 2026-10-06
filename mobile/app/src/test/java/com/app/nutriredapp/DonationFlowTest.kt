package com.app.nutriredapp

import com.app.nutriredapp.data.model.DonorType
import com.app.nutriredapp.data.repository.DonationRepository
import com.app.nutriredapp.data.repository.DonorRepository
import com.app.nutriredapp.data.repository.ProductRepository
import com.app.nutriredapp.ui.donation.DonationStep
import com.app.nutriredapp.ui.donation.DonationViewModel
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.StandardTestDispatcher
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import org.junit.After
import org.junit.Assert.*
import org.junit.Before
import org.junit.Test
import java.text.SimpleDateFormat
import java.util.*

@OptIn(ExperimentalCoroutinesApi::class)
class DonationFlowTest {

    private val testDispatcher = StandardTestDispatcher()
    private lateinit var viewModel: DonationViewModel

    @Before
    fun setUp() {
        Dispatchers.setMain(testDispatcher)
        viewModel = DonationViewModel(
            donorRepository = DonorRepository(),
            productRepository = ProductRepository(),
            donationRepository = DonationRepository()
        )
    }

    @After
    fun tearDown() {
        Dispatchers.resetMain()
    }

    @Test
    fun anonymousDonor_allowsAdvancingWithoutPersonalData() {
        viewModel.onDonorTypeChanged(DonorType.ANONYMOUS)
        val canProceed = viewModel.validateAndProceedToItems()

        assertTrue(canProceed)
        assertEquals(DonationStep.ITEMS, viewModel.uiState.value.currentStep)
        assertNull(viewModel.uiState.value.donorError)
    }

    @Test
    fun individualDonor_failsWhenMissingDocumentOrName() {
        viewModel.onDonorTypeChanged(DonorType.INDIVIDUAL)
        viewModel.onDocumentNumberChanged("")
        viewModel.onDonorNameChanged("")

        val canProceed = viewModel.validateAndProceedToItems()

        assertFalse(canProceed)
        assertNotNull(viewModel.uiState.value.donorError)
        assertEquals(DonationStep.DONOR, viewModel.uiState.value.currentStep)
    }

    @Test
    fun expirationDate_rejectsPastOrCurrentMonth() {
        val calendar = Calendar.getInstance()
        val currentMonthStr = String.format("%02d", calendar.get(Calendar.MONTH) + 1)
        val currentYearStr = calendar.get(Calendar.YEAR).toString()

        viewModel.onProductNameChanged("Arroz Test")
        viewModel.onQuantityChanged("2")
        viewModel.onExpirationMonthChanged(currentMonthStr)
        viewModel.onExpirationYearChanged(currentYearStr)

        val added = viewModel.addCurrentItemToList()

        assertFalse(added)
        assertEquals(
            "No se pueden recibir alimentos vencidos o que vencen en el mes actual.",
            viewModel.uiState.value.itemErrorMessage
        )
        assertTrue(viewModel.uiState.value.items.isEmpty())
    }

    @Test
    fun expirationDate_acceptsFutureMonthAndYear() {
        val calendar = Calendar.getInstance().apply { add(Calendar.MONTH, 6) }
        val futureMonthStr = String.format("%02d", calendar.get(Calendar.MONTH) + 1)
        val futureYearStr = calendar.get(Calendar.YEAR).toString()

        viewModel.onProductNameChanged("Lentejas Secas")
        viewModel.onQuantityChanged("5")
        viewModel.onExpirationMonthChanged(futureMonthStr)
        viewModel.onExpirationYearChanged(futureYearStr)

        val added = viewModel.addCurrentItemToList()

        assertTrue(added)
        assertNull(viewModel.uiState.value.itemErrorMessage)
        assertEquals(1, viewModel.uiState.value.items.size)
        assertEquals("Lentejas Secas", viewModel.uiState.value.items.first().productName)
        assertEquals(5.0, viewModel.uiState.value.items.first().quantity, 0.01)
    }

    @Test
    fun quantity_rejectsZeroOrNegative() {
        val calendar = Calendar.getInstance().apply {
            add(Calendar.MONTH, 6)
        }
        val futureDateStr = SimpleDateFormat("dd/MM/yyyy", Locale.getDefault()).format(calendar.time)

        viewModel.onProductNameChanged("Aceite")
        viewModel.onQuantityChanged("0")
        viewModel.onExpirationDateChanged(futureDateStr)

        val added = viewModel.addCurrentItemToList()

        assertFalse(added)
        assertEquals("Ingrese una cantidad válida y mayor a cero.", viewModel.uiState.value.itemErrorMessage)
    }

    @Test
    fun donationRepository_generatesFormattedCode() = runTest {
        val repo = DonationRepository()
        val donation = com.app.nutriredapp.data.model.Donation(
            volunteerId = "VOL-01",
            timestamp = "17/09/2026 10:30"
        )

        val saved = repo.saveDonation(donation)

        assertNotNull(saved.donationCode)
        assertTrue(saved.donationCode.matches(Regex("DON-\\d{4}-\\d{5}")))
    }

    @Test
    fun expirationDate_autoInsertsSlashes() {
        // Al ingresar '25' debe formatear a '25/'
        viewModel.onExpirationDateChanged("25")
        assertEquals("25/", viewModel.uiState.value.currentExpirationDate)

        // Al ingresar '25/1' debe mantenerse '25/1'
        viewModel.onExpirationDateChanged("25/1")
        assertEquals("25/1", viewModel.uiState.value.currentExpirationDate)

        // Al ingresar '25/12' debe formatear a '25/12/'
        viewModel.onExpirationDateChanged("25/12")
        assertEquals("25/12/", viewModel.uiState.value.currentExpirationDate)

        // Al ingresar año completo '25/12/2026' debe completarse '25/12/2026'
        viewModel.onExpirationDateChanged("25/12/2026")
        assertEquals("25/12/2026", viewModel.uiState.value.currentExpirationDate)
    }

    @Test
    fun unitOfMeasure_updatesProperly() {
        viewModel.onUnitOfMeasureChanged("litros")
        assertEquals("litros", viewModel.uiState.value.currentUnitOfMeasure)

        viewModel.onUnitOfMeasureChanged("gramos")
        assertEquals("gramos", viewModel.uiState.value.currentUnitOfMeasure)

        viewModel.onUnitOfMeasureChanged("otros")
        assertEquals("otros", viewModel.uiState.value.currentUnitOfMeasure)
    }
}
