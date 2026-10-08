package com.app.nutriredapp.ui.components

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.gestures.detectDragGestures
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Clear
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.graphics.StrokeJoin
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.graphics.asAndroidPath
import androidx.compose.ui.unit.sp
import java.io.ByteArrayOutputStream

fun exportSignatureBitmapBase64(paths: List<Path>, width: Int = 480, height: Int = 180): String? {
    if (paths.isEmpty()) return null
    return try {
        val bitmap = android.graphics.Bitmap.createBitmap(width, height, android.graphics.Bitmap.Config.ARGB_8888)
        val canvas = android.graphics.Canvas(bitmap)
        canvas.drawColor(android.graphics.Color.WHITE)
        val paint = android.graphics.Paint().apply {
            color = android.graphics.Color.BLACK
            strokeWidth = 6f
            style = android.graphics.Paint.Style.STROKE
            strokeCap = android.graphics.Paint.Cap.ROUND
            strokeJoin = android.graphics.Paint.Join.ROUND
            isAntiAlias = true
        }
        paths.forEach { path ->
            canvas.drawPath(path.asAndroidPath(), paint)
        }
        val stream = ByteArrayOutputStream()
        bitmap.compress(android.graphics.Bitmap.CompressFormat.PNG, 100, stream)
        val base64 = android.util.Base64.encodeToString(stream.toByteArray(), android.util.Base64.NO_WRAP)
        "data:image/png;base64,$base64"
    } catch (_: Exception) {
        null
    }
}

@Composable
fun SignaturePad(
    onSignatureChanged: (hasSignature: Boolean, signatureBase64: String?) -> Unit,
    modifier: Modifier = Modifier,
    strokeColor: Color = Color.Black,
    strokeWidth: Float = 8f
) {
    val paths = remember { mutableStateListOf<Path>() }
    var currentPath by remember { mutableStateOf<Path?>(null) }
    var drawTrigger by remember { mutableIntStateOf(0) }

    Column(
        modifier = modifier.fillMaxWidth(),
        verticalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .height(180.dp)
                .background(Color(0xFFFCFCFC), RoundedCornerShape(12.dp))
                .border(BorderStroke(2.dp, MaterialTheme.colorScheme.primary), RoundedCornerShape(12.dp))
                .pointerInput(Unit) {
                    detectDragGestures(
                        onDragStart = { offset ->
                            val path = Path().apply {
                                moveTo(offset.x, offset.y)
                            }
                            currentPath = path
                            paths.add(path)
                            drawTrigger++
                            onSignatureChanged(true, exportSignatureBitmapBase64(paths))
                        },
                        onDrag = { change, _ ->
                            change.consume()
                            currentPath?.lineTo(change.position.x, change.position.y)
                            drawTrigger++
                        },
                        onDragEnd = {
                            currentPath = null
                            val hasSig = paths.isNotEmpty()
                            onSignatureChanged(hasSig, if (hasSig) exportSignatureBitmapBase64(paths) else null)
                        },
                        onDragCancel = {
                            currentPath = null
                            val hasSig = paths.isNotEmpty()
                            onSignatureChanged(hasSig, if (hasSig) exportSignatureBitmapBase64(paths) else null)
                        }
                    )
                }
        ) {
            Canvas(modifier = Modifier.fillMaxSize()) {
                // Línea base para guiar la firma
                val lineY = size.height * 0.82f
                drawLine(
                    color = Color.LightGray,
                    start = Offset(20f, lineY),
                    end = Offset(size.width - 20f, lineY),
                    strokeWidth = 3f
                )

                // Para forzar recomposición cuando cambie drawTrigger
                if (drawTrigger >= 0) { /* trigger */ }

                paths.forEach { path ->
                    drawPath(
                        path = path,
                        color = strokeColor,
                        style = Stroke(
                            width = strokeWidth,
                            cap = StrokeCap.Round,
                            join = StrokeJoin.Round
                        )
                    )
                }
            }

            if (paths.isEmpty()) {
                Text(
                    text = "Firme aquí con el dedo",
                    color = Color.Gray,
                    fontSize = 18.sp,
                    fontWeight = FontWeight.Medium,
                    modifier = Modifier
                        .align(Alignment.BottomCenter)
                        .padding(bottom = 12.dp)
                )
            }
        }

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.End
        ) {
            OutlinedButton(
                onClick = {
                    paths.clear()
                    currentPath = null
                    drawTrigger++
                    onSignatureChanged(false, null)
                },
                shape = RoundedCornerShape(8.dp),
                contentPadding = PaddingValues(horizontal = 14.dp, vertical = 6.dp)
            ) {
                Icon(
                    imageVector = Icons.Rounded.Clear,
                    contentDescription = null,
                    modifier = Modifier.size(18.dp)
                )
                Spacer(modifier = Modifier.width(6.dp))
                Text(
                    text = "BORRAR FIRMA",
                    fontSize = 15.sp,
                    fontWeight = FontWeight.Bold
                )
            }
        }
    }
}
