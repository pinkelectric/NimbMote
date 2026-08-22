package com.bentley.remote

import android.Manifest
import android.content.Intent
import android.graphics.BitmapFactory
import android.os.Build
import android.os.Bundle
import android.provider.Settings
import androidx.activity.ComponentActivity
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.IconButton
import androidx.compose.material3.Icon
import androidx.compose.material3.TextButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Slider
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.MoreVert
import androidx.compose.material.icons.filled.PowerSettingsNew
import androidx.compose.material.icons.filled.RestartAlt
import androidx.compose.material.icons.filled.SettingsEthernet
import androidx.compose.material.icons.filled.Bedtime
import androidx.compose.material.icons.filled.Pause
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.SkipNext
import androidx.compose.material.icons.filled.SkipPrevious
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.bentley.remote.data.RemoteRepository
import com.bentley.remote.media.TimelinePredictor
import com.bentley.remote.model.AppUiState
import com.bentley.remote.service.BentleyRemoteService
import java.util.Locale
import android.os.SystemClock
import kotlinx.coroutines.delay

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        RemoteRepository.initialize(this)
        BentleyRemoteService.start(this)
        setContent {
            val notificationPermission = rememberLauncherForActivityResult(
                ActivityResultContracts.RequestPermission(),
            ) { }
            androidx.compose.runtime.LaunchedEffect(Unit) {
                if (Build.VERSION.SDK_INT >= 33) notificationPermission.launch(Manifest.permission.POST_NOTIFICATIONS)
            }
            MaterialTheme(colorScheme = if (isSystemInDarkThemeCompat()) darkColorScheme() else lightColorScheme()) {
                BentleyRemoteScreen()
            }
        }
    }
}

@Composable
private fun isSystemInDarkThemeCompat(): Boolean = androidx.compose.foundation.isSystemInDarkTheme()

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun BentleyRemoteScreen() {
    val context = LocalContext.current
    val state by RemoteRepository.state.collectAsStateWithLifecycle()
    var reverseHost by remember(state.reverseHost) { mutableStateOf(state.reverseHost) }
    var reverseEnabled by remember(state.reverseEnabled) { mutableStateOf(state.reverseEnabled) }
    var showConnectionSettings by remember { mutableStateOf(false) }

    Scaffold { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .verticalScroll(rememberScrollState())
                .padding(18.dp),
            verticalArrangement = Arrangement.spacedBy(14.dp),
        ) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Column(modifier = Modifier.weight(1f)) {
                    Text("Bentley Remote", fontSize = 28.sp, fontWeight = FontWeight.Bold)
                    Text("Windows media on this phone", color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
                StatusPill(state.connected)
            }

            Text(state.connectionLabel, style = MaterialTheme.typography.bodyMedium)
            MediaCard(state)
            VolumeCard(state)
            ComputerControlCard(state, onOpenConnectionSettings = { showConnectionSettings = true })
            PairingCard(state)

            OutlinedButton(
                onClick = { context.startActivity(Intent(Settings.ACTION_IGNORE_BATTERY_OPTIMIZATION_SETTINGS)) },
                modifier = Modifier.fillMaxWidth(),
            ) { Text("Open battery optimization settings") }
            Text(
                "No audio is played on the phone. Keep the app notification and set Battery → Unrestricted for reliable background reconnection.",
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
    }
    if (showConnectionSettings) {
        ConnectionSettingsDialog(
            reverseEnabled = reverseEnabled,
            reverseHost = reverseHost,
            onReverseEnabledChanged = { reverseEnabled = it },
            onReverseHostChanged = { reverseHost = it },
            onApply = { RemoteRepository.updateReverse(context, reverseEnabled, reverseHost) },
            onDismiss = { showConnectionSettings = false },
        )
    }
}

@Composable
private fun ConnectionSettingsDialog(
    reverseEnabled: Boolean,
    reverseHost: String,
    onReverseEnabledChanged: (Boolean) -> Unit,
    onReverseHostChanged: (String) -> Unit,
    onApply: () -> Unit,
    onDismiss: () -> Unit,
) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Дополнительные настройки подключения") },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                Text("Используйте только если One UI блокирует входящее подключение на интерфейсе hotspot. Обычно IP вручную не нужен.")
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text("Телефон подключается к Windows", modifier = Modifier.weight(1f))
                    Switch(checked = reverseEnabled, onCheckedChange = onReverseEnabledChanged)
                }
                OutlinedTextField(
                    value = reverseHost,
                    onValueChange = onReverseHostChanged,
                    label = { Text("IP Windows для аварийного режима") },
                    placeholder = { Text("Например: 192.168.43.123") },
                    singleLine = true,
                )
            }
        },
        confirmButton = {
            TextButton(onClick = { onApply(); onDismiss() }) { Text("Сохранить") }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text("Отмена") } },
    )
}

@Composable
private fun StatusPill(connected: Boolean) {
    val color = if (connected) Color(0xFF0B7A3E) else Color(0xFF8A4B00)
    Box(
        modifier = Modifier.background(color.copy(alpha = 0.14f), RoundedCornerShape(99.dp)).padding(horizontal = 12.dp, vertical = 7.dp),
    ) {
        Text(if (connected) "Connected" else "Disconnected", color = color, fontWeight = FontWeight.SemiBold)
    }
}

@Composable
private fun MediaCard(state: AppUiState) {
    val media = state.media
    Card {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
            Row(horizontalArrangement = Arrangement.spacedBy(14.dp), verticalAlignment = Alignment.CenterVertically) {
                val bitmap = remember(media.artwork) {
                    media.artwork?.let { BitmapFactory.decodeByteArray(it, 0, it.size)?.asImageBitmap() }
                }
                if (bitmap != null) {
                    Image(
                        bitmap = bitmap,
                        contentDescription = "Artwork",
                        contentScale = ContentScale.Crop,
                        modifier = Modifier.size(92.dp),
                    )
                } else {
                    Box(
                        Modifier.size(92.dp).background(MaterialTheme.colorScheme.secondaryContainer, RoundedCornerShape(10.dp)),
                        contentAlignment = Alignment.Center,
                    ) { Text("▶", fontSize = 30.sp) }
                }
                Column(modifier = Modifier.weight(1f)) {
                    Text(media.title, fontWeight = FontWeight.Bold, maxLines = 2, overflow = TextOverflow.Ellipsis)
                    if (media.artist.isNotBlank()) Text(media.artist, maxLines = 1, overflow = TextOverflow.Ellipsis)
                    Text(media.playbackStatus.replaceFirstChar { it.uppercase() }, style = MaterialTheme.typography.bodySmall)
                }
            }

            if (media.durationMs != null && media.durationMs > 0) {
                var position by remember { mutableFloatStateOf(0f) }
                var dragging by remember { mutableStateOf(false) }
                LaunchedEffect(media.positionMs, media.durationMs, media.playbackStatus, media.timelineReceivedAtElapsedMs) {
                    while (true) {
                        if (!dragging) position = TimelinePredictor.positionMs(media, SystemClock.elapsedRealtime()).toFloat()
                        if (!media.isPlaying) break
                        delay(250)
                    }
                }
                Slider(
                    value = position,
                    onValueChange = { dragging = true; position = it },
                    onValueChangeFinished = {
                        dragging = false
                        RemoteRepository.media("seek", positionMs = position.toLong())
                    },
                    valueRange = 0f..media.durationMs.toFloat().coerceAtLeast(1f),
                    enabled = state.connected && media.canSeek,
                )
                Row(Modifier.fillMaxWidth()) {
                    Text(formatTime(position.toLong()), style = MaterialTheme.typography.labelSmall)
                    Spacer(Modifier.weight(1f))
                    Text(formatTime(media.durationMs), style = MaterialTheme.typography.labelSmall)
                }
            }

            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceEvenly) {
                ControlButton("↶ 10", state.connected && media.canSeek) { RemoteRepository.media("seekBy", offsetMs = -10_000) }
                ControlIconButton(Icons.Default.SkipPrevious, "Previous", state.connected && media.canPrevious) {
                    RemoteRepository.media("previous")
                }
                ControlIconButton(
                    if (media.isPlaying) Icons.Default.Pause else Icons.Default.PlayArrow,
                    if (media.isPlaying) "Pause" else "Play",
                    state.connected && media.hasSession,
                ) {
                    RemoteRepository.media(if (media.isPlaying) "pause" else "play")
                }
                ControlIconButton(Icons.Default.SkipNext, "Next", state.connected && media.canNext) {
                    RemoteRepository.media("next")
                }
                ControlButton("10 ↷", state.connected && media.canSeek) { RemoteRepository.media("seekBy", offsetMs = 10_000) }
            }
        }
    }
}

@Composable
private fun ControlButton(label: String, enabled: Boolean, onClick: () -> Unit) {
    OutlinedButton(onClick = onClick, enabled = enabled, contentPadding = androidx.compose.foundation.layout.PaddingValues(horizontal = 10.dp)) {
        Text(label)
    }
}

@Composable
private fun ControlIconButton(icon: ImageVector, contentDescription: String, enabled: Boolean, onClick: () -> Unit) {
    OutlinedButton(onClick = onClick, enabled = enabled, contentPadding = androidx.compose.foundation.layout.PaddingValues(horizontal = 10.dp)) {
        Icon(icon, contentDescription = contentDescription, modifier = Modifier.size(24.dp))
    }
}

@Composable
private fun VolumeCard(state: AppUiState) {
    var volume by remember(state.volume.level) { mutableFloatStateOf(state.volume.level) }
    Card {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text("Windows volume", fontWeight = FontWeight.SemiBold, modifier = Modifier.weight(1f))
                Text("${(volume * 100).toInt()}%")
            }
            Slider(
                value = volume,
                onValueChange = { volume = it },
                onValueChangeFinished = { RemoteRepository.volume("set", level = volume) },
                enabled = state.connected,
            )
            Button(
                onClick = { RemoteRepository.volume("toggleMute") },
                enabled = state.connected,
                modifier = Modifier.fillMaxWidth(),
            ) { Text(if (state.volume.muted) "Unmute Windows" else "Mute Windows") }
        }
    }
}

private data class PowerUiAction(val action: String, val label: String, val confirmation: String?)

private val PrimaryPowerActions = listOf(
    PowerUiAction("shutdown", "Выключить", "Компьютер будет полностью выключен. Продолжить?"),
    PowerUiAction("restart", "Перезагрузить", "Компьютер будет перезагружен. Продолжить?"),
)
private val SecondaryPowerActions = listOf(
    PowerUiAction("sleep", "Сон", "Компьютер перейдёт в режим сна. Соединение будет прервано. Продолжить?"),
    PowerUiAction("lock", "Заблокировать", null),
)

@Composable
private fun ComputerControlCard(state: AppUiState, onOpenConnectionSettings: () -> Unit) {
    var pending by remember { mutableStateOf<PowerUiAction?>(null) }
    var menuExpanded by remember { mutableStateOf(false) }
    Card {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text("Управление компьютером", fontWeight = FontWeight.SemiBold, modifier = Modifier.weight(1f))
                Box {
                    // Connection settings must remain reachable precisely when the normal path is
                    // unavailable; only destructive power commands themselves are disabled.
                    IconButton(onClick = { menuExpanded = true }) {
                        Icon(Icons.Default.MoreVert, contentDescription = "Ещё")
                    }
                    DropdownMenu(expanded = menuExpanded, onDismissRequest = { menuExpanded = false }) {
                        SecondaryPowerActions.forEach { item ->
                            DropdownMenuItem(
                                text = { Text(item.label) },
                                leadingIcon = {
                                    Icon(
                                        if (item.action == "sleep") Icons.Default.Bedtime else Icons.Default.Lock,
                                        contentDescription = null,
                                    )
                                },
                                enabled = state.connected,
                                onClick = {
                                    menuExpanded = false
                                    if (item.confirmation == null) RemoteRepository.systemAction(item.action) else pending = item
                                },
                            )
                        }
                        DropdownMenuItem(
                            text = { Text("Дополнительные настройки подключения") },
                            leadingIcon = { Icon(Icons.Default.SettingsEthernet, contentDescription = null) },
                            enabled = true,
                            onClick = { menuExpanded = false; onOpenConnectionSettings() },
                        )
                    }
                }
            }
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(10.dp)) {
                PrimaryPowerActions.forEach { item ->
                    Button(
                        onClick = { pending = item },
                        enabled = state.connected,
                        modifier = Modifier.weight(1f),
                    ) {
                        Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                            Icon(
                                if (item.action == "shutdown") Icons.Default.PowerSettingsNew else Icons.Default.RestartAlt,
                                contentDescription = null,
                            )
                            Text(item.label)
                        }
                    }
                }
            }
            if (!state.connected) Text("Доступно после защищённого подключения", style = MaterialTheme.typography.bodySmall)
            state.commandResult?.let { Text(it, style = MaterialTheme.typography.bodySmall) }
        }
    }
    pending?.let { item ->
        AlertDialog(
            onDismissRequest = { pending = null },
            title = { Text(item.label) },
            text = { Text(item.confirmation.orEmpty()) },
            confirmButton = { TextButton(onClick = { pending = null; RemoteRepository.systemAction(item.action) }) { Text("Продолжить") } },
            dismissButton = { TextButton(onClick = { pending = null }) { Text("Отмена") } },
        )
    }
}

@Composable
private fun PairingCard(state: AppUiState) {
    var code by remember { mutableStateOf("") }
    Card(colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.primaryContainer)) {
        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
            Text("Secure pairing", fontWeight = FontWeight.SemiBold)
            if (state.pairedComputer == null) {
                OutlinedTextField(
                    value = code,
                    onValueChange = { value -> code = value.filter(Char::isDigit).take(6) },
                    label = { Text("6-digit code from Windows") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth(),
                )
                Button(
                    onClick = { RemoteRepository.beginPairing(code) },
                    enabled = code.length == 6,
                    modifier = Modifier.fillMaxWidth(),
                ) { Text("Search and pair on this LAN") }
                Text("Open Pair with phone in the Windows tray first. No IP address is required.")
                OutlinedButton(onClick = { RemoteRepository.resetPairing() }) { Text("Reset pairing search") }
            } else {
                Text("Paired with ${state.pairedComputer}")
                OutlinedButton(onClick = { RemoteRepository.resetPairing() }) { Text("Forget computer and show new code") }
            }
        }
    }
}

private fun formatTime(milliseconds: Long): String {
    val seconds = (milliseconds / 1_000).coerceAtLeast(0)
    return String.format(Locale.US, "%d:%02d", seconds / 60, seconds % 60)
}
