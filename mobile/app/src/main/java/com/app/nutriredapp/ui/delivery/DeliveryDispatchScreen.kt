package com.app.nutriredapp.ui.delivery

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.ArrowBack
import androidx.compose.material.icons.automirrored.rounded.ArrowForward
import androidx.compose.material.icons.rounded.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.app.nutriredapp.data.model.FoodPackage
import com.app.nutriredapp.ui.components.AccessibleButton
import com.app.nutriredapp.ui.components.AccessibleTextField
import com.app.nutriredapp.ui.components.SignaturePad

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DeliveryDispatchScreen(
    onBackToHome: () -> Unit,
    viewModel: DeliveryViewModel = viewModel()
) {
    val uiState by viewModel.uiState.collectAsState()

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        text = "Despacho de Entregas",
                        fontSize = 24.sp,
                        lineHeight = 30.sp,
                        fontWeight = FontWeight.ExtraBold
                    )
                },
                navigationIcon = {
                    IconButton(
                        onClick = {
                            if (uiState.selectedPackage != null) {
                                viewModel.clearSelection()
                            } else {
                                onBackToHome()
                            }
                        }
                    ) {
                        Icon(
                            imageVector = Icons.AutoMirrored.Rounded.ArrowBack,
                            contentDescription = "Volver",
                            modifier = Modifier.size(34.dp)
                        )
                    }
                },
                colors = TopAppBarDefaults.topAppBarColors(
                    containerColor = MaterialTheme.colorScheme.primaryContainer,
                    titleContentColor = MaterialTheme.colorScheme.onPrimaryContainer
                ),
                actions = {
                    if (uiState.selectedPackage == null && uiState.completedPackage == null) {
                        IconButton(onClick = { viewModel.loadPackages() }) {
                            Icon(
                                imageVector = Icons.Rounded.Refresh,
                                contentDescription = "Actualizar paquetes desde base de datos",
                                modifier = Modifier.size(30.dp)
                            )
                        }
                    }
                }
            )
        }
    ) { innerPadding ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(innerPadding)
        ) {
            when {
                uiState.completedPackage != null -> {
                    DeliveryReceiptView(
                        completed = uiState.completedPackage!!,
                        onNewDelivery = { viewModel.clearSelection() },
                        onBackToHome = onBackToHome,
                        modifier = Modifier.verticalScroll(rememberScrollState())
                    )
                }
                uiState.selectedPackage != null -> {
                    DeliveryConfirmationForm(
                        pkg = uiState.selectedPackage!!,
                        uiState = uiState,
                        viewModel = viewModel,
                        modifier = Modifier.verticalScroll(rememberScrollState())
                    )
                }
                else -> {
                    PreparedPackagesListView(
                        packages = uiState.preparedPackages,
                        isLoading = uiState.isLoading,
                        onRefresh = { viewModel.loadPackages() },
                        onSelectPackage = { viewModel.selectPackage(it) }
                    )
                }
            }
        }
    }
}

/**
 * 1. Lista de paquetes preparados para entrega
 */
@Composable
fun PreparedPackagesListView(
    packages: List<FoodPackage>,
    isLoading: Boolean,
    onRefresh: () -> Unit,
    onSelectPackage: (FoodPackage) -> Unit,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier
            .fillMaxSize()
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                text = "Paquetes Preparados (${packages.size})",
                fontSize = 22.sp,
                lineHeight = 28.sp,
                fontWeight = FontWeight.ExtraBold,
                color = MaterialTheme.colorScheme.onSurface
            )
        }

        if (isLoading) {
            LinearProgressIndicator(modifier = Modifier.fillMaxWidth())
        }

        if (packages.isEmpty() && !isLoading) {
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(vertical = 32.dp),
                contentAlignment = Alignment.Center
            ) {
                Column(
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.spacedBy(16.dp)
                ) {
                    Text(
                        text = "No hay paquetes pendientes de entrega.",
                        fontSize = 18.sp,
                        lineHeight = 24.sp,
                        textAlign = TextAlign.Center,
                        color = Color.DarkGray
                    )
                    OutlinedButton(onClick = onRefresh) {
                        Icon(Icons.Rounded.Refresh, contentDescription = null, modifier = Modifier.size(20.dp))
                        Spacer(modifier = Modifier.width(8.dp))
                        Text("Recargar desde Supabase", fontSize = 16.sp)
                    }
                }
            }
        } else {
            LazyColumn(
                modifier = Modifier.fillMaxSize(),
                verticalArrangement = Arrangement.spacedBy(14.dp)
            ) {
                items(packages) { pkg ->
                    PackageCard(pkg = pkg, onSelect = { onSelectPackage(pkg) })
                }
            }
        }
    }
}

@Composable
fun PackageCard(
    pkg: FoodPackage,
    onSelect: () -> Unit
) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant),
        border = BorderStroke(1.5.dp, MaterialTheme.colorScheme.primary)
    ) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = pkg.packageCode,
                    fontSize = 22.sp,
                    lineHeight = 28.sp,
                    fontWeight = FontWeight.Black,
                    color = MaterialTheme.colorScheme.primary
                )
                Surface(
                    color = Color(0xFFE8F5E9),
                    shape = RoundedCornerShape(8.dp)
                ) {
                    Text(
                        text = "PREPARADO",
                        fontSize = 14.sp,
                        fontWeight = FontWeight.Bold,
                        color = Color(0xFF2E7D32),
                        modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp)
                    )
                }
            }

            Text(
                text = "Familia: ${pkg.familyTitularName}",
                fontSize = 20.sp,
                lineHeight = 26.sp,
                fontWeight = FontWeight.Bold
            )

            Text(
                text = "DNI Titular: ${pkg.familyTitularDni} • ${pkg.familyMembersCount} integrantes",
                fontSize = 17.sp,
                lineHeight = 23.sp,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )

            Text(
                text = "Contenido: ${pkg.items.size} tipos de alimentos (${pkg.items.sumOf { it.quantity }.toInt()} unidades)",
                fontSize = 16.sp,
                lineHeight = 22.sp,
                fontWeight = FontWeight.SemiBold,
                color = MaterialTheme.colorScheme.primary
            )

            AccessibleButton(
                text = "DESPACHAR Y FIRMAR",
                icon = Icons.AutoMirrored.Rounded.ArrowForward,
                onClick = onSelect,
                minHeight = 58.dp
            )
        }
    }
}

/**
 * 2. Formulario de validación de identidad y firma digital (RF-3)
 */
@Composable
fun DeliveryConfirmationForm(
    pkg: FoodPackage,
    uiState: DeliveryUiState,
    viewModel: DeliveryViewModel,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier
            .fillMaxWidth()
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        // Tarjeta resumen del paquete a entregar
        Card(
            modifier = Modifier.fillMaxWidth(),
            shape = RoundedCornerShape(14.dp),
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.primaryContainer)
        ) {
            Column(
                modifier = Modifier.padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(6.dp)
            ) {
                Text(
                    text = "ENTREGA DE PAQUETE: ${pkg.packageCode}",
                    fontSize = 16.sp,
                    lineHeight = 22.sp,
                    fontWeight = FontWeight.Black,
                    color = MaterialTheme.colorScheme.onPrimaryContainer
                )
                Text(
                    text = "Titular: ${pkg.familyTitularName}",
                    fontSize = 22.sp,
                    lineHeight = 28.sp,
                    fontWeight = FontWeight.Bold
                )
                Text(
                    text = "DNI Titular registrado: ${pkg.familyTitularDni}",
                    fontSize = 18.sp,
                    lineHeight = 24.sp
                )
            }
        }

        // Validación de identidad
        Text(
            text = "Validación de Identidad del Receptor:",
            fontSize = 20.sp,
            lineHeight = 26.sp,
            fontWeight = FontWeight.Bold
        )

        AccessibleTextField(
            value = uiState.receiverDni,
            onValueChange = { viewModel.onReceiverDniChanged(it) },
            label = "DNI de quien retira *",
            placeholder = "Ingrese DNI para verificar",
            leadingIcon = Icons.Rounded.Badge,
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
            isError = uiState.errorMessage?.contains("DNI") == true || uiState.errorMessage?.contains("identidad") == true,
            errorMessage = if (uiState.errorMessage?.contains("DNI") == true || uiState.errorMessage?.contains("identidad") == true) uiState.errorMessage else null
        )

        AccessibleTextField(
            value = uiState.receiverName,
            onValueChange = { viewModel.onReceiverNameChanged(it) },
            label = "Nombre y Apellido del Receptor *",
            placeholder = "Nombre completo",
            leadingIcon = Icons.Rounded.Person
        )

        // Captura de Firma Digital en Pantalla
        Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
            Text(
                text = "Firma Digital de Conformidad *",
                fontSize = 18.sp,
                lineHeight = 24.sp,
                fontWeight = FontWeight.SemiBold
            )
            SignaturePad(
                onSignatureChanged = { viewModel.onSignatureChanged(it) }
            )
        }

        val err = uiState.errorMessage
        if (err != null && !err.contains("DNI") && !err.contains("identidad")) {
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                Icon(
                    imageVector = Icons.Rounded.Warning,
                    contentDescription = null,
                    tint = MaterialTheme.colorScheme.error,
                    modifier = Modifier.size(24.dp)
                )
                Text(
                    text = err,
                    color = MaterialTheme.colorScheme.error,
                    fontSize = 16.sp,
                    lineHeight = 22.sp,
                    fontWeight = FontWeight.Bold
                )
            }
        }

        AccessibleButton(
            text = "CONFIRMAR ENTREGA Y FIRMA",
            icon = Icons.Rounded.CheckCircle,
            onClick = { viewModel.confirmDelivery() },
            enabled = !uiState.isSubmitting
        )

        AccessibleButton(
            text = "CANCELAR / VOLVER",
            icon = Icons.Rounded.Close,
            onClick = { viewModel.clearSelection() },
            containerColor = Color.LightGray,
            contentColor = Color.Black,
            minHeight = 56.dp
        )
    }
}

/**
 * 3. Comprobante digital de entrega con estado ENTREGADO inmutable
 */
@Composable
fun DeliveryReceiptView(
    completed: FoodPackage,
    onNewDelivery: () -> Unit,
    onBackToHome: () -> Unit,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier
            .fillMaxWidth()
            .padding(16.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(18.dp)
    ) {
        Surface(
            shape = CircleShape,
            color = Color(0xFF2E7D32),
            modifier = Modifier.size(76.dp)
        ) {
            Box(contentAlignment = Alignment.Center) {
                Icon(
                    imageVector = Icons.Rounded.Check,
                    contentDescription = null,
                    tint = Color.White,
                    modifier = Modifier.size(46.dp)
                )
            }
        }

        Text(
            text = "¡Entrega Concretada!",
            fontSize = 28.sp,
            lineHeight = 34.sp,
            fontWeight = FontWeight.Black,
            color = Color(0xFF2E7D32),
            textAlign = TextAlign.Center
        )

        Text(
            text = "El paquete ha quedado en estado inmutable ENTREGADO.",
            fontSize = 18.sp,
            lineHeight = 24.sp,
            textAlign = TextAlign.Center
        )

        Card(
            modifier = Modifier.fillMaxWidth(),
            shape = RoundedCornerShape(16.dp),
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant),
            border = BorderStroke(2.dp, Color(0xFF2E7D32))
        ) {
            Column(
                modifier = Modifier.padding(20.dp),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                Text(
                    text = "CONSTANCIA DIGITAL DE DESPACHO",
                    fontSize = 15.sp,
                    lineHeight = 20.sp,
                    fontWeight = FontWeight.Black,
                    color = Color(0xFF2E7D32)
                )

                Text(
                    text = completed.packageCode,
                    fontSize = 30.sp,
                    lineHeight = 36.sp,
                    fontWeight = FontWeight.ExtraBold
                )

                HorizontalDivider()

                Text(
                    text = "Fecha y hora: ${completed.deliveredAt ?: "Ahora"}",
                    fontSize = 18.sp,
                    lineHeight = 24.sp
                )
                Text(
                    text = "Titular: ${completed.familyTitularName}",
                    fontSize = 18.sp,
                    lineHeight = 24.sp
                )
                Text(
                    text = "Retirado por: ${completed.receiverName} (DNI: ${completed.receiverDni})",
                    fontSize = 18.sp,
                    lineHeight = 24.sp,
                    fontWeight = FontWeight.Bold
                )
                Text(
                    text = "Voluntario responsable: ${completed.volunteerName ?: "Voluntario"}",
                    fontSize = 17.sp,
                    lineHeight = 23.sp
                )

                HorizontalDivider()

                Text(
                    text = "Firma digital del beneficiario: REGISTRADA ✓",
                    fontSize = 17.sp,
                    lineHeight = 23.sp,
                    fontWeight = FontWeight.Bold,
                    color = Color(0xFF2E7D32)
                )

                Text(
                    text = "Alimentos entregados (${completed.items.size}):",
                    fontSize = 17.sp,
                    lineHeight = 23.sp,
                    fontWeight = FontWeight.Bold
                )

                completed.items.forEach { item ->
                    Text(
                        text = "• ${item.productName}: ${item.quantity} ${item.unitOfMeasure} (Vence: ${item.expirationDate})",
                        fontSize = 16.sp,
                        lineHeight = 22.sp,
                        color = Color.DarkGray
                    )
                }
            }
        }

        Spacer(modifier = Modifier.height(8.dp))

        AccessibleButton(
            text = "DESPACHAR OTRO PAQUETE",
            icon = Icons.Rounded.LocalShipping,
            onClick = onNewDelivery
        )

        AccessibleButton(
            text = "VOLVER AL MENÚ PRINCIPAL",
            icon = Icons.Rounded.Home,
            onClick = onBackToHome,
            containerColor = MaterialTheme.colorScheme.secondary,
            minHeight = 58.dp
        )
    }
}
