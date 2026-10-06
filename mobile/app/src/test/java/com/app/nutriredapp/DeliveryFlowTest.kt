package com.app.nutriredapp

import com.app.nutriredapp.data.model.PackageStatus
import com.app.nutriredapp.data.repository.DeliveryRepository
import com.app.nutriredapp.ui.delivery.DeliveryViewModel
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.test.StandardTestDispatcher
import kotlinx.coroutines.test.advanceUntilIdle
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.runTest
import kotlinx.coroutines.test.setMain
import org.junit.After
import org.junit.Assert.*
import org.junit.Before
import org.junit.Test

@OptIn(ExperimentalCoroutinesApi::class)
class DeliveryFlowTest {

    private val testDispatcher = StandardTestDispatcher()
    private lateinit var repository: DeliveryRepository
    private lateinit var viewModel: DeliveryViewModel

    @Before
    fun setUp() {
        Dispatchers.setMain(testDispatcher)
        repository = DeliveryRepository(testDispatcher)
        viewModel = DeliveryViewModel(repository)
    }

    @After
    fun tearDown() {
        Dispatchers.resetMain()
    }

    @Test
    fun loadPreparedPackages_returnsInitialPreparedList() = runTest {
        advanceUntilIdle()
        val packages = viewModel.uiState.value.preparedPackages
        assertTrue(packages.isNotEmpty())
        assertTrue(packages.all { it.status == PackageStatus.PREPARADO })
    }

    @Test
    fun delivery_failsWhenDniDiscrepancy() = runTest {
        advanceUntilIdle()
        val pkg = viewModel.uiState.value.preparedPackages.first()
        viewModel.selectPackage(pkg)

        viewModel.onReceiverDniChanged("99999999") // DNI incorrecto
        viewModel.onReceiverNameChanged("Persona Desconocida")
        viewModel.onSignatureChanged(true)

        viewModel.confirmDelivery()
        advanceUntilIdle()

        assertNotNull(viewModel.uiState.value.errorMessage)
        assertTrue(viewModel.uiState.value.errorMessage!!.contains("Discrepancia de identidad"))
        assertNull(viewModel.uiState.value.completedPackage)
    }

    @Test
    fun delivery_failsWhenSignatureMissing() = runTest {
        advanceUntilIdle()
        val pkg = viewModel.uiState.value.preparedPackages.first()
        viewModel.selectPackage(pkg)

        viewModel.onReceiverDniChanged(pkg.familyTitularDni)
        viewModel.onReceiverNameChanged(pkg.familyTitularName)
        viewModel.onSignatureChanged(false) // Sin firma

        viewModel.confirmDelivery()
        advanceUntilIdle()

        assertNotNull(viewModel.uiState.value.errorMessage)
        assertTrue(viewModel.uiState.value.errorMessage!!.contains("Firma digital vacía"))
        assertNull(viewModel.uiState.value.completedPackage)
    }

    @Test
    fun delivery_succeedsWithMatchingDniAndSignature() = runTest {
        advanceUntilIdle()
        val pkg = viewModel.uiState.value.preparedPackages.first()
        viewModel.selectPackage(pkg)

        viewModel.onReceiverDniChanged(pkg.familyTitularDni)
        viewModel.onReceiverNameChanged(pkg.familyTitularName)
        viewModel.onSignatureChanged(true) // Con firma

        viewModel.confirmDelivery()
        advanceUntilIdle()

        val completed = viewModel.uiState.value.completedPackage
        assertNotNull(completed)
        assertEquals(PackageStatus.ENTREGADO, completed!!.status)
        assertEquals(pkg.familyTitularDni, completed.receiverDni)
        assertTrue(completed.hasSignature)
        assertNotNull(completed.deliveredAt)
    }
}
