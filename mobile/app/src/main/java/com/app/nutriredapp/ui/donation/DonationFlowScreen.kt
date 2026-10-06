package com.app.nutriredapp.ui.donation

import android.Manifest
import android.util.Log
import androidx.annotation.OptIn as AndroidxOptIn
import androidx.camera.core.*
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.camera.view.PreviewView
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.ArrowBack
import androidx.compose.material.icons.rounded.Close
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.content.ContextCompat
import androidx.lifecycle.compose.LocalLifecycleOwner
import androidx.lifecycle.viewmodel.compose.viewModel
import com.app.nutriredapp.ui.components.AccessibleButton
import com.app.nutriredapp.ui.components.StepIndicator
import com.google.accompanist.permissions.ExperimentalPermissionsApi
import com.google.accompanist.permissions.isGranted
import com.google.accompanist.permissions.rememberPermissionState
import com.google.mlkit.vision.barcode.BarcodeScanner
import com.google.mlkit.vision.barcode.BarcodeScanning
import com.google.mlkit.vision.common.InputImage

@OptIn(ExperimentalMaterial3Api::class, ExperimentalPermissionsApi::class)
@Composable
fun DonationFlowScreen(
    onBackToHome: () -> Unit,
    viewModel: DonationViewModel = viewModel()
) {
    val uiState by viewModel.uiState.collectAsState()
    val cameraPermissionState = rememberPermissionState(Manifest.permission.CAMERA)

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        text = "Recepción de Donaciones",
                        fontSize = 24.sp,
                        fontWeight = FontWeight.ExtraBold
                    )
                },
                navigationIcon = {
                    IconButton(
                        onClick = {
                            when (uiState.currentStep) {
                                DonationStep.DONOR -> onBackToHome()
                                DonationStep.ITEMS -> viewModel.goToStep(DonationStep.DONOR)
                                DonationStep.SUMMARY -> viewModel.goToStep(DonationStep.ITEMS)
                                DonationStep.RECEIPT -> onBackToHome()
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
                )
            )
        }
    ) { paddingValues ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(paddingValues)
        ) {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .verticalScroll(rememberScrollState())
            ) {
                // Indicador cognitivo de pasos (WCAG)
                if (uiState.currentStep != DonationStep.RECEIPT) {
                    StepIndicator(
                        currentStep = uiState.currentStep.stepNumber,
                        totalSteps = 3,
                        stepTitle = uiState.currentStep.title,
                        modifier = Modifier.padding(16.dp)
                    )
                }

                when (uiState.currentStep) {
                    DonationStep.DONOR -> {
                        DonorStep(
                            uiState = uiState,
                            viewModel = viewModel
                        )
                    }
                    DonationStep.ITEMS -> {
                        ItemsStep(
                            uiState = uiState,
                            viewModel = viewModel,
                            onOpenScanner = {
                                if (cameraPermissionState.status.isGranted) {
                                    viewModel.setCameraScanning(true)
                                } else {
                                    cameraPermissionState.launchPermissionRequest()
                                }
                            }
                        )
                    }
                    DonationStep.SUMMARY -> {
                        SummaryStep(
                            uiState = uiState,
                            viewModel = viewModel,
                            onBackToItems = { viewModel.goToStep(DonationStep.ITEMS) }
                        )
                    }
                    DonationStep.RECEIPT -> {
                        ReceiptStep(
                            uiState = uiState,
                            onNewDonation = { viewModel.resetForNewDonation() },
                            onBackToHome = onBackToHome
                        )
                    }
                }
            }

            // Escáner de código de barras superpuesto accesible
            if (uiState.isCameraScanning) {
                BarcodeScannerOverlay(
                    onBarcodeFound = { barcode ->
                        viewModel.onBarcodeScanned(barcode)
                    },
                    onClose = {
                        viewModel.setCameraScanning(false)
                    }
                )
            }
        }
    }
}

@Composable
fun BarcodeScannerOverlay(
    onBarcodeFound: (String) -> Unit,
    onClose: () -> Unit
) {
    val context = LocalContext.current
    val lifecycleOwner = LocalLifecycleOwner.current
    val cameraProviderFuture = remember { ProcessCameraProvider.getInstance(context) }
    var detectedBarcode by remember { mutableStateOf<String?>(null) }

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
                        processBarcodeProxy(scanner, imageProxy) { barcode ->
                            if (detectedBarcode == null) {
                                detectedBarcode = barcode
                                onBarcodeFound(barcode)
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
                        Log.e("ScannerOverlay", "Camera binding failed", e)
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
                color = Color.Black.copy(alpha = 0.75f),
                shape = RoundedCornerShape(12.dp),
                modifier = Modifier.fillMaxWidth()
            ) {
                Text(
                    text = "Apunta la cámara hacia el código de barras del alimento",
                    color = Color.White,
                    fontSize = 20.sp,
                    fontWeight = FontWeight.Bold,
                    textAlign = TextAlign.Center,
                    modifier = Modifier.padding(16.dp)
                )
            }

            AccessibleButton(
                text = "CANCELAR ESCANEO",
                icon = Icons.Rounded.Close,
                onClick = onClose,
                containerColor = MaterialTheme.colorScheme.error,
                minHeight = 64.dp
            )
        }
    }
}

@AndroidxOptIn(ExperimentalGetImage::class)
private fun processBarcodeProxy(
    scanner: BarcodeScanner,
    imageProxy: ImageProxy,
    onBarcodeFound: (String) -> Unit
) {
    val mediaImage = imageProxy.image
    if (mediaImage != null) {
        val image = InputImage.fromMediaImage(mediaImage, imageProxy.imageInfo.rotationDegrees)
        scanner.process(image)
            .addOnSuccessListener { barcodes ->
                for (barcode in barcodes) {
                    barcode.rawValue?.let {
                        onBarcodeFound(it)
                        return@addOnSuccessListener
                    }
                }
            }
            .addOnFailureListener {
                Log.e("Scanner", "Error escaneando código", it)
            }
            .addOnCompleteListener {
                imageProxy.close()
            }
    } else {
        imageProxy.close()
    }
}
