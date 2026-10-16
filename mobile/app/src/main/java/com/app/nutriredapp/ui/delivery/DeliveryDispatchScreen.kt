package com.app.nutriredapp.ui.delivery

import android.Manifest
import android.util.Log
import androidx.annotation.OptIn as AndroidxOptIn
import androidx.camera.core.*
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.camera.view.PreviewView
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
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
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.hapticfeedback.HapticFeedbackType
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalHapticFeedback
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.content.ContextCompat
import androidx.lifecycle.compose.LocalLifecycleOwner
import androidx.lifecycle.viewmodel.compose.viewModel
import com.app.nutriredapp.data.model.FoodPackage
import com.app.nutriredapp.ui.components.AccessibleButton
import com.app.nutriredapp.ui.components.AccessibleTextField
import com.app.nutriredapp.ui.components.SignaturePad
import com.google.accompanist.permissions.ExperimentalPermissionsApi
import com.google.accompanist.permissions.isGranted
import com.google.accompanist.permissions.rememberPermissionState
import com.google.mlkit.vision.barcode.BarcodeScanner
import com.google.mlkit.vision.barcode.BarcodeScanning
import com.google.mlkit.vision.common.InputImage

@OptIn(ExperimentalMaterial3Api::class, ExperimentalPermissionsApi::class)
@Composable
fun DeliveryDispatchScreen(
    onBackToHome: () -> Unit,
    viewModel: DeliveryViewModel = viewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    val cameraPermissionState = rememberPermissionState(Manifest.permission.CAMERA)
    val haptic = LocalHapticFeedback.current

    DisposableEffect(Unit) {
        onDispose {
            viewModel.clearSelection()
        }
    }

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
                            if (uiState.isQrScanning) {
                                viewModel.setQrScanning(false)
                            } else if (uiState.selectedPackage != null || uiState.completedPackage != null) {
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
                    if (uiState.selectedPackage == null && uiState.completedPackage == null && !uiState.isQrScanning) {
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
                .consumeWindowInsets(innerPadding)
                .imePadding()
        ) {
            when {
                uiState.completedPackage != null -> {
                    DeliveryReceiptView(
                        completed = uiState.completedPackage!!,
                        onNewDelivery = { viewModel.clearSelection() },
                        onBackToHome = {
                            viewModel.clearSelection()
                            onBackToHome()
                        },
                        modifier = Modifier.verticalScroll(rememberScrollState())
                    )
                }
                uiState.selectedPackage != null -> {
                    DeliveryConfirmationForm(
                        pkg = uiState.selectedPackage!!,
                        uiState = uiState,
                        viewModel = viewModel,
                        modifier = Modifier
                            .verticalScroll(rememberScrollState())
                            .padding(bottom = 32.dp)
                    )
                }
                else -> {
                    PreparedPackagesListView(
                        packages = uiState.preparedPackages,
                        isLoading = uiState.isLoading,
                        errorMessage = uiState.errorMessage,
                        failedMessage = uiState.failedDeliveryMessage,
                        onDismissFailedMessage = { viewModel.dismissFailedDeliveryMessage() },
                        onRefresh = { viewModel.loadPackages() },
                        onSelectPackage = { viewModel.selectPackage(it) },
                        onScanQrClick = {
                            haptic.performHapticFeedback(HapticFeedbackType.LongPress)
                            if (cameraPermissionState.status.isGranted) {
                                viewModel.setQrScanning(true)
                            } else {
                                cameraPermissionState.launchPermissionRequest()
                            }
                        }
                    )
                }
            }

            // Escáner de código QR superpuesto
            if (uiState.isQrScanning) {
                QrScannerOverlay(
                    onQrFound = { qrCode ->
                        haptic.performHapticFeedback(HapticFeedbackType.LongPress)
                        viewModel.onQrScanned(qrCode)
                    },
                    onClose = {
                        viewModel.setQrScanning(false)
                    }
                )
            }
        }
    }
}

/**
 * 1. Lista de paquetes preparados para entrega + Botón de Escáner QR
 */
@Composable
fun PreparedPackagesListView(
    packages: List<FoodPackage>,
    isLoading: Boolean,
    errorMessage: String?,
    failedMessage: String?,
    onDismissFailedMessage: () -> Unit,
    onRefresh: () -> Unit,
    onSelectPackage: (FoodPackage) -> Unit,
    onScanQrClick: () -> Unit,
    modifier: Modifier = Modifier
) {

    Column(
        modifier = modifier
            .fillMaxSize()
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        // Banner de aviso de no entrega
        if (!failedMessage.isNullOrBlank()) {
            Surface(
                shape = RoundedCornerShape(12.dp),
                color = MaterialTheme.colorScheme.errorContainer,
                border = BorderStroke(1.dp, MaterialTheme.colorScheme.error)
            ) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 14.dp, vertical = 10.dp),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    Icon(
                        imageVector = Icons.Rounded.Cancel,
                        contentDescription = null,
                        tint = MaterialTheme.colorScheme.onErrorContainer,
                        modifier = Modifier.size(24.dp)
                    )
                    Text(
                        text = failedMessage,
                        fontSize = 15.sp,
                        lineHeight = 20.sp,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.onErrorContainer,
                        modifier = Modifier.weight(1f)
                    )
                    IconButton(onClick = onDismissFailedMessage) {
                        Icon(
                            imageVector = Icons.Rounded.Close,
                            contentDescription = "Cerrar aviso",
                            tint = MaterialTheme.colorScheme.onErrorContainer
                        )
                    }
                }
            }
        }

        // Mensaje de error (ej. paquete no encontrado o no habilitado)
        if (!errorMessage.isNullOrBlank()) {
            Surface(
                shape = RoundedCornerShape(12.dp),
                color = MaterialTheme.colorScheme.errorContainer,
                border = BorderStroke(1.dp, MaterialTheme.colorScheme.error)
            ) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 14.dp, vertical = 10.dp),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    Icon(
                        imageVector = Icons.Rounded.Warning,
                        contentDescription = null,
                        tint = MaterialTheme.colorScheme.onErrorContainer,
                        modifier = Modifier.size(24.dp)
                    )
                    Text(
                        text = errorMessage,
                        fontSize = 15.sp,
                        lineHeight = 20.sp,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.onErrorContainer,
                        modifier = Modifier.weight(1f)
                    )
                }
            }
        }

        // BOTÓN PRINCIPAL: ESCANEAR CÓDIGO QR DE LA CAJA (Generado en la web de despacho)
        AccessibleButton(
            text = "ESCANEAR CÓDIGO QR DEL PAQUETE",
            icon = Icons.Rounded.QrCodeScanner,
            onClick = onScanQrClick,
            containerColor = MaterialTheme.colorScheme.primary,
            contentColor = MaterialTheme.colorScheme.onPrimary,
            minHeight = 68.dp
        )

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column {
                Text(
                    text = "Paquetes Preparados",
                    fontSize = 22.sp,
                    lineHeight = 28.sp,
                    fontWeight = FontWeight.Bold
                )
                Text(
                    text = "${packages.size} disponibles en depósito",
                    fontSize = 15.sp,
                    lineHeight = 20.sp,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        }

        if (isLoading) {
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .weight(1f),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator()
            }
        } else if (packages.isEmpty()) {
            Card(
                modifier = Modifier
                    .fillMaxWidth()
                    .weight(1f),
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant)
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(24.dp),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.Center
                ) {
                    Icon(
                        imageVector = Icons.Rounded.Inventory2,
                        contentDescription = null,
                        modifier = Modifier.size(64.dp),
                        tint = MaterialTheme.colorScheme.primary
                    )
                    Spacer(modifier = Modifier.height(16.dp))
                    Text(
                        text = "No hay paquetes preparados pendientes.",
                        fontSize = 20.sp,
                        lineHeight = 26.sp,
                        fontWeight = FontWeight.Bold,
                        textAlign = TextAlign.Center
                    )
                    Spacer(modifier = Modifier.height(8.dp))
                    Text(
                        text = "Puedes pulsar 'Escanear Código QR' para buscar cualquier paquete individualmente por su código QR.",
                        fontSize = 16.sp,
                        lineHeight = 22.sp,
                        textAlign = TextAlign.Center,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    Spacer(modifier = Modifier.height(16.dp))
                    OutlinedButton(onClick = onRefresh) {
                        Icon(Icons.Rounded.Refresh, contentDescription = null)
                        Spacer(modifier = Modifier.width(8.dp))
                        Text("Actualizar lista", fontSize = 16.sp)
                    }
                }
            }
        } else {
            LazyColumn(
                modifier = Modifier.weight(1f),
                verticalArrangement = Arrangement.spacedBy(14.dp)
            ) {
                items(packages, key = { it.id }) { pkg ->
                    PackageCardItem(
                        pkg = pkg,
                        onClick = { onSelectPackage(pkg) }
                    )
                }
            }
        }
    }
}

/**
 * Overlay accesible de Cámara para Escaneo de Códigos QR
 */
@Composable
fun QrScannerOverlay(
    onQrFound: (String) -> Unit,
    onClose: () -> Unit
) {
    val context = LocalContext.current
    val lifecycleOwner = LocalLifecycleOwner.current
    val cameraProviderFuture = remember { ProcessCameraProvider.getInstance(context) }
    var detectedQr by remember { mutableStateOf<String?>(null) }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.Black.copy(alpha = 0.95f))
    ) {
        AndroidView(
            factory = { ctx ->
                val previewView = PreviewView(ctx)
                val executor = ContextCompat.getMainExecutor(ctx)

                cameraProviderFuture.addListener({
                    val cameraProvider = cameraProviderFuture.get()
                    val preview = Preview.Builder().build().also {
                        it.surfaceProvider = previewView.surfaceProvider
                    }

                    val scanner = BarcodeScanning.getClient()
                    val imageAnalysis = ImageAnalysis.Builder()
                        .setBackpressureStrategy(ImageAnalysis.STRATEGY_KEEP_ONLY_LATEST)
                        .build()

                    imageAnalysis.setAnalyzer(executor) { imageProxy ->
                        processQrProxy(scanner, imageProxy) { qrCode ->
                            if (detectedQr == null) {
                                detectedQr = qrCode
                                onQrFound(qrCode)
                            }
                        }
                    }

                    val cameraSelector = CameraSelector.DEFAULT_BACK_CAMERA

                    try {
                        cameraProvider.unbindAll()
                        cameraProvider.bindToLifecycle(
                            lifecycleOwner,
                            cameraSelector,
                            preview,
                            imageAnalysis
                        )
                    } catch (e: Exception) {
                        Log.e("QrScannerOverlay", "Camera binding failed", e)
                    }
                }, executor)
                previewView
            },
            modifier = Modifier.fillMaxSize()
        )

        // Overlay con instrucciones accesibles y botón de cancelar
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(24.dp),
            verticalArrangement = Arrangement.SpaceBetween,
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Surface(
                color = Color.Black.copy(alpha = 0.8f),
                shape = RoundedCornerShape(14.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(
                    modifier = Modifier.padding(18.dp),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.spacedBy(6.dp)
                ) {
                    Icon(
                        imageVector = Icons.Rounded.QrCodeScanner,
                        contentDescription = null,
                        tint = Color.White,
                        modifier = Modifier.size(40.dp)
                    )
                    Text(
                        text = "Apunta la cámara al Código QR del Paquete",
                        color = Color.White,
                        fontSize = 20.sp,
                        fontWeight = FontWeight.Bold,
                        textAlign = TextAlign.Center
                    )
                    Text(
                        text = "Generado en la web de despacho del sistema",
                        color = Color.LightGray,
                        fontSize = 15.sp,
                        textAlign = TextAlign.Center
                    )
                }
            }

            AccessibleButton(
                text = "CANCELAR ESCANEO QR",
                icon = Icons.Rounded.Close,
                onClick = onClose,
                containerColor = MaterialTheme.colorScheme.error,
                minHeight = 64.dp
            )
        }
    }
}

@AndroidxOptIn(ExperimentalGetImage::class)
private fun processQrProxy(
    scanner: BarcodeScanner,
    imageProxy: ImageProxy,
    onQrFound: (String) -> Unit
) {
    val mediaImage = imageProxy.image
    if (mediaImage != null) {
        val image = InputImage.fromMediaImage(mediaImage, imageProxy.imageInfo.rotationDegrees)
        scanner.process(image)
            .addOnSuccessListener { barcodes ->
                for (barcode in barcodes) {
                    barcode.rawValue?.let {
                        onQrFound(it)
                        return@addOnSuccessListener
                    }
                }
            }
            .addOnFailureListener {
                Log.e("QrScanner", "Error escaneando código QR", it)
            }
            .addOnCompleteListener {
                imageProxy.close()
            }
    } else {
        imageProxy.close()
    }
}

@Composable
fun PackageCardItem(
    pkg: FoodPackage,
    onClick: () -> Unit
) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .clickable { onClick() },
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant),
        border = BorderStroke(1.dp, MaterialTheme.colorScheme.outlineVariant)
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(18.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Column(modifier = Modifier.weight(1f)) {
                Surface(
                    shape = RoundedCornerShape(6.dp),
                    color = MaterialTheme.colorScheme.primary
                ) {
                    Text(
                        text = pkg.packageCode,
                        modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp),
                        color = MaterialTheme.colorScheme.onPrimary,
                        fontSize = 14.sp,
                        fontWeight = FontWeight.Black
                    )
                }

                Spacer(modifier = Modifier.height(8.dp))

                Text(
                    text = pkg.familyTitularName,
                    fontSize = 20.sp,
                    lineHeight = 26.sp,
                    fontWeight = FontWeight.Bold
                )

                Text(
                    text = "DNI: ${pkg.familyTitularDni} • Grupo: ${pkg.familyMembersCount} integrantes",
                    fontSize = 16.sp,
                    lineHeight = 22.sp,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )

                Spacer(modifier = Modifier.height(4.dp))

                Text(
                    text = "${pkg.items.size} tipos de alimentos incluidos",
                    fontSize = 15.sp,
                    fontWeight = FontWeight.Medium,
                    color = MaterialTheme.colorScheme.primary
                )
            }

            Icon(
                imageVector = Icons.AutoMirrored.Rounded.ArrowForward,
                contentDescription = "Despachar",
                tint = MaterialTheme.colorScheme.primary,
                modifier = Modifier.size(36.dp)
            )
        }
    }
}

/**
 * 2. Formulario de confirmación y captura de firma digital para despacho
 */
@Composable
fun DeliveryConfirmationForm(
    pkg: FoodPackage,
    uiState: DeliveryUiState,
    viewModel: DeliveryViewModel,
    modifier: Modifier = Modifier
) {
    var showFailedDialog by remember { mutableStateOf(false) }
    var selectedReason by remember { mutableStateOf("Titular ausente en domicilio") }
    var customReason by remember { mutableStateOf("") }

    val reasonOptions = listOf(
        "Titular ausente en domicilio",
        "Discrepancia de identidad / Sin DNI",
        "Rechazo del paquete por el beneficiario",
        "Domicilio inaccesible / No localizado",
        "Otro motivo..."
    )

    if (showFailedDialog) {
        AlertDialog(
            onDismissRequest = { showFailedDialog = false },
            icon = {
                Icon(
                    imageVector = Icons.Rounded.Warning,
                    contentDescription = null,
                    tint = MaterialTheme.colorScheme.error,
                    modifier = Modifier.size(36.dp)
                )
            },
            title = {
                Text(
                    text = "Entrega No Concretada",
                    fontSize = 20.sp,
                    fontWeight = FontWeight.Bold,
                    textAlign = TextAlign.Center
                )
            },
            text = {
                Column(
                    modifier = Modifier.fillMaxWidth(),
                    verticalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    Text(
                        text = "¿Por qué no se pudo entregar el paquete ${pkg.packageCode} a ${pkg.familyTitularName}?",
                        fontSize = 15.sp,
                        lineHeight = 20.sp,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )

                    reasonOptions.forEach { reason ->
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .clickable { selectedReason = reason }
                                .padding(vertical = 4.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            RadioButton(
                                selected = (selectedReason == reason),
                                onClick = { selectedReason = reason }
                            )
                            Spacer(modifier = Modifier.width(8.dp))
                            Text(
                                text = reason,
                                fontSize = 15.sp,
                                fontWeight = if (selectedReason == reason) FontWeight.Bold else FontWeight.Normal
                            )
                        }
                    }

                    if (selectedReason == "Otro motivo...") {
                        OutlinedTextField(
                            value = customReason,
                            onValueChange = { customReason = it },
                            label = { Text("Especifique el motivo") },
                            placeholder = { Text("Detalle de la imposibilidad de entrega") },
                            modifier = Modifier.fillMaxWidth(),
                            singleLine = false,
                            maxLines = 3
                        )
                    }
                }
            },
            confirmButton = {
                Button(
                    onClick = {
                        val finalReason = if (selectedReason == "Otro motivo...") {
                            customReason.ifBlank { "Entrega no concretada sin detalle" }
                        } else {
                            selectedReason
                        }
                        showFailedDialog = false
                        viewModel.registerFailedDelivery(finalReason)
                    },
                    colors = ButtonDefaults.buttonColors(containerColor = MaterialTheme.colorScheme.error)
                ) {
                    Text("REGISTRAR FALLO", fontWeight = FontWeight.Bold)
                }
            },
            dismissButton = {
                TextButton(onClick = { showFailedDialog = false }) {
                    Text("CANCELAR", fontWeight = FontWeight.Bold)
                }
            }
        )
    }

    Column(
        modifier = modifier
            .fillMaxWidth()
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        // Resumen del Paquete a Entregar
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
                    text = "ENTREGANDO: ${pkg.packageCode}",
                    fontSize = 20.sp,
                    lineHeight = 26.sp,
                    fontWeight = FontWeight.Black,
                    color = MaterialTheme.colorScheme.onPrimaryContainer
                )
                Text(
                    text = "Familia: ${pkg.familyTitularName} (DNI ${pkg.familyTitularDni})",
                    fontSize = 17.sp,
                    lineHeight = 23.sp,
                    color = MaterialTheme.colorScheme.onPrimaryContainer
                )
                Text(
                    text = "Grupo conviviente: ${pkg.familyMembersCount} personas",
                    fontSize = 15.sp,
                    color = MaterialTheme.colorScheme.onPrimaryContainer
                )
            }
        }

        Text(
            text = "Contenido del Paquete:",
            fontSize = 18.sp,
            lineHeight = 24.sp,
            fontWeight = FontWeight.Bold
        )

        Card(
            modifier = Modifier.fillMaxWidth(),
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant)
        ) {
            Column(
                modifier = Modifier.padding(12.dp),
                verticalArrangement = Arrangement.spacedBy(6.dp)
            ) {
                pkg.items.forEach { item ->
                    Text(
                        text = "• ${item.productName}: ${item.quantity} ${item.unitOfMeasure} (Vence: ${item.expirationDate})",
                        fontSize = 16.sp,
                        lineHeight = 22.sp
                    )
                }
            }
        }

        HorizontalDivider(modifier = Modifier.padding(vertical = 4.dp))

        Text(
            text = "Datos de Conformidad de Recepción",
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
                onSignatureChanged = { hasSig, b64 -> viewModel.onSignatureChanged(hasSig, b64) }
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
            isLoading = uiState.isSubmitting,
            enabled = !uiState.isSubmitting
        )

        AccessibleButton(
            text = "REGISTRAR ENTREGA NO CONCRETADA",
            icon = Icons.Rounded.Cancel,
            onClick = { showFailedDialog = true },
            containerColor = MaterialTheme.colorScheme.error,
            contentColor = MaterialTheme.colorScheme.onError,
            enabled = !uiState.isSubmitting,
            minHeight = 58.dp
        )

        AccessibleButton(
            text = "VOLVER / CANCELAR",
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
