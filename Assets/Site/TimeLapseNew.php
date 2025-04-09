<?php
// Directory containing the game data
$dir = 'Timelapse';
$gameNumber = 19;
$filename = $dir . "/Game_{$gameNumber}_MapHistory.txt";

// Read the file content
if (!file_exists($filename)) {
    die("Game data file not found.");
}

$lines = file($filename, FILE_IGNORE_NEW_LINES | FILE_SKIP_EMPTY_LINES);
if (count($lines) === 0) {
    die("No data found in the game data file.");
}

// Parse the data
$mapHistory = [];
foreach ($lines as $line) {
    list($date, $mapData) = explode('_', $line);
    $mapHistory[] = ['date' => $date, 'mapData' => $mapData];
}

// Encode map history as a JSON array for use in JavaScript
$mapHistoryJson = json_encode($mapHistory);
?>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Time-lapse Viewer</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            text-align: center;
        }
        #viewer {
            width: 80%;
            max-width: 1000px;
            margin: 20px auto;
            position: relative;
        }
        #map {
            width: 100%;
            max-width: 100%;
            height: auto;
            border: 1px solid #ccc;
            margin-top: 10px;
            position: relative;
        }
        .tile {
            position: absolute;
            box-sizing: border-box;
            border: 1px solid #ccc;
        }
        #slider {
            width: 100%;
        }
        #dateDisplay {
            margin-top: 10px;
        }
    </style>
</head>
<body>

<h1>Time-lapse Viewer</h1>
<div id="viewer">
    <select id="gameNumber">
        <option value="19">Game 19</option>
        <!-- Add more game numbers here if needed -->
    </select>
    <div id="map">
        <img id="backgroundImage" src="path/to/your/map/image.png" alt="Map Image">
    </div>
    <input type="range" id="slider" min="0" max="<?php echo count($mapHistory) - 1; ?>" value="0">
    <div id="dateDisplay"></div>
</div>

<script>
    const mapHistory = <?php echo $mapHistoryJson; ?>;
    const slider = document.getElementById('slider');
    const dateDisplay = document.getElementById('dateDisplay');
    const map = document.getElementById('map');
    const backgroundImage = document.getElementById('backgroundImage');
    const tileSize = backgroundImage.width / 50;

    function updateMap(step) {
        const { date, mapData } = mapHistory[step];
        dateDisplay.textContent = date;

        // Clear existing tiles
        const existingTiles = document.querySelectorAll('.tile');
        existingTiles.forEach(tile => tile.remove());

        // Create new tiles
        for (let i = 0; i < mapData.length; i++) {
            const tile = document.createElement('div');
            tile.className = 'tile';
            tile.style.width = `${tileSize}px`;
            tile.style.height = `${tileSize}px`;
            tile.style.left = `${(i % 50) * tileSize}px`;
            tile.style.top = `${Math.floor(i / 50) * tileSize}px`;

            switch (mapData[i]) {
                case '1':
                    tile.style.backgroundColor = 'red';
                    break;
                case '2':
                    tile.style.backgroundColor = 'blue';
                    break;
                case '3':
                    tile.style.backgroundColor = 'green';
                    break;
                case '4':
                    tile.style.backgroundColor = 'yellow';
                    break;
                default:
                    tile.style.backgroundColor = 'transparent';
            }

            map.appendChild(tile);
        }
    }

    slider.addEventListener('input', function() {
        updateMap(this.value);
    });

    document.addEventListener('keydown', function(event) {
        if (event.key === 'ArrowLeft' && slider.value > 0) {
            slider.value--;
            updateMap(slider.value);
        } else if (event.key === 'ArrowRight' && slider.value < slider.max) {
            slider.value++;
            updateMap(slider.value);
        }
    });

    // Initial map load
    updateMap(0);
</script>

</body>
</html>
