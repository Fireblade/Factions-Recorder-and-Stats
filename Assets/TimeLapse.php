<?php
header("Cache-Control: no-store, no-cache, must-revalidate, max-age=0");
header("Cache-Control: post-check=0, pre-check=0", false);
header("Pragma: no-cache");

// Directory containing the game data
$dir = 'TimeLapse';

// Load configuration
$config = require 'config.php';

$servername = $config['servername'];
$username = $config['username'];
$password = $config['password'];
$dbname = $config['dbname'];


// Scan the directory for available games
$files = scandir($dir);
$games = [];
foreach ($files as $file) {
    if (preg_match('/^(TestGame|Game)_(\d+)_Data\.txt$/', $file, $matches)) {
        $games[] = ['prefix' => $matches[1], 'number' => $matches[2]];
    }
}

// Get the selected game from the query parameter or default to the first game
$selectedGamePrefix = isset($_GET['gamePrefix']) ? $_GET['gamePrefix'] : $games[0]['prefix'];
$selectedGameNumber = isset($_GET['gameNumber']) ? $_GET['gameNumber'] : $games[0]['number'];
$selectedStepNumber = isset($_GET['minute']) ? $_GET['minute'] : -1;
$firstLoad = true;

$filename = $dir . "/{$selectedGamePrefix}_{$selectedGameNumber}_MapHistory.txt";

// Log the received data to the console
echo "<script>console.log(" . json_encode($mapDataLines) . ");</script>";

// Create connection
$conn = new mysqli($servername, $username, $password, $dbname);

// Check connection
if ($conn->connect_error) {
    die("Connection failed: " . $conn->connect_error);
}

$sql = "SELECT * FROM ActiveGames WHERE GameID = ?";
$stmt = $conn->prepare($sql);
$stmt->bind_param("i", $selectedGameNumber);
$stmt->execute();
$result = $stmt->get_result();

if ($result->num_rows > 0) {
    $row = $result->fetch_assoc();
    $mapName = $row['Map'];
    $mapWidth = (int)$row['MapWidth'];
    $mapHeight = (int)$row['MapHeight'];
    $gameActive = filter_var($row['Active'], FILTER_VALIDATE_BOOLEAN);
    $gameWinGoal = (int)$row['VictoryGoal'];
    $gameBuildingRevision = (int)$row['BuildingCostsVersion'];
    $gameHQCostScaling = [
        (float)$row['HQ_Iron_Multi'],
        (float)$row['HQ_Wood_Multi'],
        (float)$row['HQ_Worker_Multi']
    ];
    $gameBuildingCostScaling = [
        (float)$row['Building_Iron_Multi'],
        (float)$row['Building_Wood_Multi'],
        (float)$row['Building_Worker_Multi']
    ];
} else {
    die("Game data not found.");
}

$stmt->close();
$conn->close();

$suppliedTransparency = 40;
$unsuppliedTransparency = 20;
$buildingsMaxShowLevel = 25;

// Debug statement to print the extracted values
// echo "<pre>";
// print_r($gameHQCostScaling);
// print_r($gameBuildingCostScaling);
// echo "</pre>";

$buildingsMaxShowLevel = 25;

// Read the game data file
if (!file_exists($filename)) {
    die("Game data file not found.");
}

if (file_exists("Buildings/" . $gameBuildingRevision . ".txt")) {
    $buildingDataFile = "Buildings/" . $gameBuildingRevision . ".txt";
    $buildingLines = file($buildingDataFile, FILE_IGNORE_NEW_LINES | FILE_SKIP_EMPTY_LINES);

    $buildings = [];
    foreach ($buildingLines as $line) {
        list($buildingName, $woodCost, $ironCost, $workerCost, $woodStart, $ironStart, $workerStart) = explode(',', $line);
        $buildings[] = [
            'name' => $buildingName,
            'woodCost' => (int)$woodCost,
            'ironCost' => (int)$ironCost,
            'workerCost' => floatval($workerCost),
            'woodStart' => (int)$woodStart,
            'ironStart' => (int)$ironStart,
            'workerStart' => (int)$workerStart
        ];
    }

    // Debug statement to print the contents of the $buildings array
    //echo "<pre>";
    //print_r($buildings);
    //echo "</pre>";
}

$lines = file($filename, FILE_IGNORE_NEW_LINES | FILE_SKIP_EMPTY_LINES);
if (count($lines) === 0) {
    die("This game has not started yet. Please come back when this game has started.");
}

// Parse the data
$mapHistory = [];
foreach ($lines as $line) {
    list($date, $mapData, $teamPoints, $teamPointsGain, $teamSentSoldiers, $teamSoldierGainMinute, $teamSoldierGainHour, $teamSentWorkers, $teamWorkersGainMinute, $teamWorkersGainHour, $teamAverageHQ, $teamHQOver10, $teamHQOver15, $teamConnectedPlayers) = array_pad(explode('_', $line), 14, '0');
    
    $mapHistory[] = [
        'date' => $date,
        'mapData' => $mapData,
        'teamPoints' => array_map('intval', explode('=', $teamPoints) + [0, 0, 0, 0]),
        'teamPointsGain' => array_map('intval', explode('=', $teamPointsGain) + [0, 0, 0, 0]),
        'teamSentSoldiers' => array_map('intval', explode('=', $teamSentSoldiers) + [0, 0, 0, 0]),
        'teamSoldierGainMinute' => array_map(function($value) { return round(floatval($value), 2); }, explode('=', $teamSoldierGainMinute) + [0, 0, 0, 0]),
        'teamSoldierGainHour' => array_map(function($value) { return round(floatval($value), 2); }, explode('=', $teamSoldierGainHour) + [0, 0, 0, 0]),
        'teamSentWorkers' => array_map('intval', explode('=', $teamSentWorkers) + [0, 0, 0, 0]),
        'teamWorkersGainMinute' => array_map(function($value) { return round(floatval($value), 2); }, explode('=', $teamWorkersGainMinute) + [0, 0, 0, 0]),
        'teamWorkersGainHour' => array_map(function($value) { return round(floatval($value), 2); }, explode('=', $teamWorkersGainHour) + [0, 0, 0, 0]),
        'teamAverageHQ' => array_map(function($value) { return round(floatval($value), 2); }, explode('=', $teamAverageHQ) + [0, 0, 0, 0]),
        'teamHQOver10' => array_map('intval', explode('=', $teamHQOver10) + [0, 0, 0, 0]),
        'teamHQOver15' => array_map('intval', explode('=', $teamHQOver15) + [0, 0, 0, 0]),
        'teamConnectedPlayers' => array_map('intval', explode('=', $teamConnectedPlayers) + [0, 0, 0, 0])
    ];
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
            margin: 0;
            padding: 0;
            height: 100vh;
            display: flex;
            flex-direction: column;
        }
        #container {
            display: flex;
            flex-direction: row;
            height: 100%;
        }
        #viewer {
            #viewer {
                width: 60%;
                min-width: 40%;
                height: 100%;
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: flex-start;
            }
            width: 67%;
            height: 100%;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: flex-start;
        }
        #map {
            width: fit-content;
            aspect-ratio: 1;
            height: 80%;
            border: 1px solid #ccc;
            position: relative;
        }
        .tile {
            position: absolute;
            box-sizing: border-box;
            border: 1px solid rgba(0, 0, 0, 0.5);
        }
        .tile.transparent {
            border: none;
        }
        #gameNumberContainer {
            display: flex;
            align-items: center;
            width: 80%;
            justify-content: space-between;
        }
        #gameNumber {
            font-size: 1.2em;
        }
        #status {
            font-size: 1.2em;
            margin-left: 10px;
        }
        #slider {
            margin-top: 10px;
            width: 100%;
        }
         #gameExtraInfo {
            display: flex;
            align-items: center;
            width: 80%;
            justify-content: space-between;
        }
         #gameExtraInfoLine2 {
            display: flex;
            align-items: center;
            width: 80%;
            justify-content: space-between;
        }
        #dateDisplay {
            margin-top: 10px;
            align-self: flex-start;
        }
        #gridToggle {
            margin-top: 10px;
            align-self: flex-end;
        }
        #instructions {
            margin-top: 10px;
            font-size: 0.9em;
            color: #555;
            align-self: flex-start;
        }
        #autoplay {
            margin-top: 10px;
            font-size: 0.9em;
            color: #555;
            align-self: flex-end;
        }
        #autoplay label, #autoplay select {
            display: inline-block;
        }
        #infoPanel {
            width: 40%;
            min-width: 33%;
            padding: 10px;
            display: flex;
            flex-direction: column;
            overflow-y: auto;
        }
        #buttons {
            display: flex;
            justify-content: space-around;
            margin-bottom: 6px;
        }
        #overview, #buildings {
            display: none;
        }
        #overview.active, #buildings.active {
            display: block;
        }
        .team-table {
            width: 100%;
            margin-bottom: 6px;
            font-size: 0.9em;
        }
        .team-table th, .team-table td {
            padding: 2px;
            text-align: left;
        }
        .status {
            font-weight: bold;
            color: green;
        }
        .sticky-column {
            position: -webkit-sticky; /* For Safari */
            position: sticky;
            left: 0;
            background-color: white; /* Optional: to ensure the background is consistent */
            z-index: 1; /* Ensure it stays on top of other content */
        }
        .odd-row {
            background-color: #f2f2f2; /* Slightly darker background for odd rows */
        }
        .wood-column {
            background-color: #f5deb3; /* Light brown for wood columns */
        }
        .iron-column {
            background-color: #add8e6; /* Light blue for iron columns */
        }
        .worker-column {
            background-color: #ffffe0; /* Light yellow for worker columns */
        }
    </style>
</head>
<body>

<div id="container">
    <div id="viewer">
        <div id="gameNumberContainer">
            <select id="gameNumber" onchange="updateGame()">
                <?php foreach ($games as $game): ?>
                    <option value="<?php echo $game['prefix'] . '_' . $game['number']; ?>" <?php echo ($game['prefix'] === $selectedGamePrefix && $game['number'] == $selectedGameNumber) ? 'selected' : ''; ?>>
                        <?php echo $game['prefix'] . ' ' . $game['number']; ?>
                    </option>
                <?php endforeach; ?>
            </select>
            <div id="status" class="status">Downloading data...</div>
        </div>
        <div id="map">
            <img id="backgroundImage" src="Maps/<?php echo $mapName . ".png"; ?>" alt="Map Image" style="height: 100%; width: auto;">
        </div>
        <input type="range" id="slider" min="0" max="<?php echo count($mapHistory) - 1; ?>" value="<?php echo count($mapHistory) - 1; ?>">
        <div id="gameExtraInfo">
            <div id="dateDisplay"></div>
            <div id="gridToggle">
                <div>
                    <label for="toggleGrid">Show Grid Lines</label>
                    <input type="checkbox" id="toggleGrid">
                </div>
            </div>
        </div>
        <div id="gameExtraInfoLine2">
            <div id="instructions">Use arrow keys to seek as well</div>
            <div id="autoplay">
            <label for="autoplaySelect">Auto-Play:</label>
            <select id="autoplaySelect" onchange="updateAutoplay()">
                <option value="off">Off</option>
                <option value="1">1x</option>
                <option value="5">5x</option>
                <option value="10">10x</option>
                <option value="25">25x</option>
                <option value="50">50x</option>
                <option value="100">100x</option>
            </select>
            </div>
        </div>
    </div>
    <div id="infoPanel">
        <div id="buttons">
            <button onclick="showSection('overview')">Overview</button>
            <button onclick="showSection('buildings')">Buildings</button>
        </div>
        <div id="overview" class="active">
            <table class="team-table" style="border: 1px solid black;">
                <thead>
                    <?php
                    $teams = ['RED', 'BLUE', 'GREEN', 'YELLOW'];
                    ?>
                    <table class="team-table" style="border: 1px solid black;">
                        <thead>
                            <?php foreach ($teams as $team): ?>
                                <tr style="background-color: <?php echo ($team === 'BLUE' || $team === 'GREEN') ? 'light' . strtolower($team) : strtolower($team); ?>; border: 1px solid black;">
                                    <th id="teamNameAndConnected_<?php echo $team; ?>" style="text-align: center;"><?php echo $team . ' (' . $teamConnectedPlayers . ')'; ?></th>
                                    <th id="teamPoints_<?php echo $team; ?>" style="text-align: center;"> (+<span id="teamPointsGain_<?php echo $team; ?>"></span>)</th>
                                </tr>
                                <tr style="border: 1px solid black;">
                                    <td>Sent Soldiers</td>
                                    <td id="teamSentSoldiers_<?php echo $team; ?>"></td>
                                    <td>
                                        <div id="teamSoldierGainMinute_<?php echo $team; ?>">/Min</div>
                                        <div id="teamSoldierGainHour_<?php echo $team; ?>">/Hour</div>
                                    </td>
                                </tr>
                                <tr style="border: 1px solid black;">
                                    <td>Sent Workers</td>
                                    <td id="teamSentWorkers_<?php echo $team; ?>"></td>
                                    <td>
                                        <div id="teamWorkersGainMinute_<?php echo $team; ?>">/Min</div>
                                        <div id="teamWorkersGainHour_<?php echo $team; ?>">/Hour</div>
                                    </td>
                                </tr>
                                <tr style="border: 1px solid black;">
                                    <td>Top 20 Average HQ Level</td>
                                    <td id="teamAverageHQ_<?php echo $team; ?>"></td>
                                </tr>
                                <tr style="border: 1px solid black;">
                                    <td>Players HQ10+</td>
                                    <td id="teamHQOver10_<?php echo $team; ?>"></td>
                                </tr>
                                <tr style="border: 1px solid black;">
                                    <td>Players HQ15+</td>
                                    <td id="teamHQOver15_<?php echo $team; ?>"></td>
                                </tr>
                                <tr style="border: 1px solid black;">
                                    <td>Time till VP Win</td>
                                    <td id="timeTillVPWin_<?php echo $team; ?>"></td>
                                </tr>
                                <tr style="border: 1px solid black;">
                                    <td colspan="3"><hr></td>
                                </tr>
                            <?php endforeach; ?>
                        </thead>
                    </table>
                </thead>
            </table>
        </div>

        <div id="buildings" style="overflow: auto;">
            <p style="font-size: 100%;">* Does not include any current, if any, Economic Scaling Event changes.</p>
            <table border="1" id="buildingsTable">
                <tr>
                    <th class="sticky-column">Level</th>
                    <?php foreach ($buildings as $building): ?>
                        <th colspan="3"><?php echo htmlspecialchars($building['name']); ?></th>
                    <?php endforeach; ?>
                </tr>
                <tr>
                    <td class="sticky-column"></td>
                    <?php foreach ($buildings as $building): ?>
                        <td class="wood-column">Wood Cost</td>
                        <td class="iron-column">Iron Cost</td>
                        <td class="worker-column">Worker Cost</td>
                    <?php endforeach; ?>
                </tr>
                <?php for ($level = 1; $level <= $buildingsMaxShowLevel; $level++): ?>
                    <tr class="<?php echo $level % 2 == 1 ? 'odd-row' : ''; ?>">
                        <td class="sticky-column"><?php echo $level; ?></td>
                        <?php foreach ($buildings as $building): ?>
                            <?php
                            if ($building['name'] === 'HQ') {
                                $woodCost = $level >= $building['woodStart'] ? $building['woodCost'] * pow($gameHQCostScaling[0], $level - 1) : 0;
                                $ironCost = $level >= $building['ironStart'] ? $building['ironCost'] * pow($gameHQCostScaling[1], $level - 1) : 0;
                                $workerCost = $level >= $building['workerStart'] ? $building['workerCost'] * pow($gameHQCostScaling[2], $level - 1) : 0;
                            } else {
                                $woodCost = $level >= $building['woodStart'] ? $building['woodCost'] * pow($gameBuildingCostScaling[0], $level - 1) : 0;
                                $ironCost = $level >= $building['ironStart'] ? $building['ironCost'] * pow($gameBuildingCostScaling[1], $level - 1) : 0;
                                $workerCost = $level >= $building['workerStart'] ? $building['workerCost'] * pow($gameBuildingCostScaling[2], $level - 1) : 0;
                            }

                            ?>
                            <td class="wood-column"><?php echo number_format($woodCost); ?></td>
                            <td class="iron-column"><?php echo number_format($ironCost); ?></td>
                            <td class="worker-column"><?php echo number_format($workerCost); ?></td>
                        <?php endforeach; ?>
                    </tr>
                <?php endfor; ?>
            </table>
            
        <button id="showMoreLevels" onclick="showMoreLevels()">Show 5 more levels</button>
        </div>

    </div>
<script>
    let mapHistory = <?php echo $mapHistoryJson; ?>;
    var selectedStepNumber = <?php echo $selectedStepNumber; ?>;
    const slider = document.getElementById('slider');
    const dateDisplay = document.getElementById('dateDisplay');
    const map = document.getElementById('map');
    const backgroundImage = document.getElementById('backgroundImage');
    const toggleGrid = document.getElementById('toggleGrid');
    const mapWidth = <?php echo $mapWidth; ?>;
    const mapHeight = <?php echo $mapHeight; ?>;
    const gameActive = <?php echo json_encode($gameActive); ?>;
	const gameWinGoal = <?php echo json_encode($gameWinGoal); ?>;
    let lastDate = '0';
    let autoplayInterval = null;
    let currentIndex = mapHistory.length - 1;

    console.log(`Initial slider value: ${slider.value}, Max value: ${slider.max}`);

    function updateMap(step) {

        let data = mapHistory[step];
        let { date, mapData } = mapHistory[step];
        dateDisplay.textContent = date + " Game Minute: " + step;

        const teams = ['RED', 'BLUE', 'GREEN', 'YELLOW'];
        for (let i = 0; i < teams.length; i++) {
            if (document.getElementById(`teamPoints_${teams[i]}`)) {
                let suffix = " (+ 0/minute)";
                if (data.teamPointsGain[i] >= 0) {
                    suffix = " (+ " + data.teamPointsGain[i] + "/minute)";
                }
                document.getElementById(`teamPoints_${teams[i]}`).innerText = data.teamPoints[i].toLocaleString() + suffix;
            }

            if (document.getElementById(`teamNameAndConnected_${teams[i]}`)) {
                document.getElementById(`teamNameAndConnected_${teams[i]}`).innerText = `${teams[i]} (${data.teamConnectedPlayers[i].toLocaleString()})`;
            }
            if (document.getElementById(`teamPointsGain_${teams[i]}`)) {
                document.getElementById(`teamPointsGain_${teams[i]}`).innerText = data.teamPointsGain[i].toLocaleString();
            }
            if (document.getElementById(`teamSentSoldiers_${teams[i]}`)) {
                document.getElementById(`teamSentSoldiers_${teams[i]}`).innerText = data.teamSentSoldiers[i].toLocaleString();
            }
            if (document.getElementById(`teamSoldierGainMinute_${teams[i]}`)) {
                document.getElementById(`teamSoldierGainMinute_${teams[i]}`).innerText = data.teamSoldierGainMinute[i].toLocaleString() + ' /Last Minute';
            }
            if (document.getElementById(`teamSoldierGainHour_${teams[i]}`)) {
                document.getElementById(`teamSoldierGainHour_${teams[i]}`).innerText = data.teamSoldierGainHour[i].toLocaleString() + ' /Last Hour';
            }
            if (document.getElementById(`teamSentWorkers_${teams[i]}`)) {
                document.getElementById(`teamSentWorkers_${teams[i]}`).innerText = data.teamSentWorkers[i].toLocaleString();
            }
            if (document.getElementById(`teamWorkersGainMinute_${teams[i]}`)) {
                document.getElementById(`teamWorkersGainMinute_${teams[i]}`).innerText = data.teamWorkersGainMinute[i].toLocaleString() + ' /Last Minute';
            }
            if (document.getElementById(`teamWorkersGainHour_${teams[i]}`)) {
                document.getElementById(`teamWorkersGainHour_${teams[i]}`).innerText = data.teamWorkersGainHour[i].toLocaleString() + ' /Last Hour';
            }
            if (document.getElementById(`teamAverageHQ_${teams[i]}`)) {
                document.getElementById(`teamAverageHQ_${teams[i]}`).innerText = data.teamAverageHQ[i];
            }
            if (document.getElementById(`teamHQOver10_${teams[i]}`)) {
                document.getElementById(`teamHQOver10_${teams[i]}`).innerText = data.teamHQOver10[i];
            }
            if (document.getElementById(`teamHQOver15_${teams[i]}`)) {
                document.getElementById(`teamHQOver15_${teams[i]}`).innerText = data.teamHQOver15[i];
            }
            if (document.getElementById(`timeTillVPWin_${teams[i]}`)) {
                if(data.teamPointsGain[i] == 0) {
                    document.getElementById(`timeTillVPWin_${teams[i]}`).innerText = 'Infinite - Not gaining points';
                } else {
                    if (typeof gameWinGoal === 'undefined') {
                        document.getElementById(`timeTillVPWin_${teams[i]}`).innerText = 'Infinite - Not gaining points';
                    }
                    else{
                        let minutesTillWin = gameWinGoal / data.teamPointsGain[i];
						let hours = Math.floor(minutesTillWin / 60);
						let minutes = Math.floor(minutesTillWin % 60);
						document.getElementById(`timeTillVPWin_${teams[i]}`).innerText = `${hours} hours, ${minutes} minutes`;
                    }
                }
            }
        }

        if (step == slider.max && gameActive) {
            document.getElementById('status').innerText = 'Live!';
        }
        else{
            document.getElementById('status').innerText = '';
        }

        // Clear existing tiles
        const existingTiles = document.querySelectorAll('.tile');
        existingTiles.forEach(tile => tile.remove());

        // Calculate tile size based on the actual image dimensions
        const tileSizeX = backgroundImage.clientWidth / mapWidth;
        const tileSizeY = backgroundImage.clientHeight / mapHeight;

        // Create new tiles
        for (let i = 0; i < mapData.length; i++) {
            const tile = document.createElement('div');
            tile.className = 'tile';
            tile.style.width = `${tileSizeX}px`;
            tile.style.height = `${tileSizeY}px`;
            tile.style.left = `${(i % mapWidth) * tileSizeX}px`;
            tile.style.top = `${Math.floor(i / mapWidth) * tileSizeY}px`;

            switch (mapData[i]) {
                case '1':
                    tile.style.backgroundColor = 'rgba(255, 0, 0, 0.40)';
                    break;
                case '2':
                    tile.style.backgroundColor = 'rgba(0, 0, 255, 0.40)';
                    break;
                case '3':
                    tile.style.backgroundColor = 'rgba(0, 255, 0, 0.40)';
                    break;
                case '4':
                    tile.style.backgroundColor = 'rgba(255, 255, 0, 0.40)';
                    break;
                case '5':
                    tile.style.backgroundColor = 'rgba(225, 0, 0, 0.25)';
                    break;
                case '6':
                    tile.style.backgroundColor = 'rgba(0, 0, 225, 0.25)';
                    break;
                case '7':
                    tile.style.backgroundColor = 'rgba(0, 225, 0, 0.25)';
                    break;
                case '8':
                    tile.style.backgroundColor = 'rgba(225, 225, 0, 0.25)';
                    break;
                case 'N':
                    tile.style.backgroundColor = 'rgba(225, 225, 255, 0.25)';
                    break;
                default:
                    tile.style.backgroundColor = 'transparent';
            }

            if (!toggleGrid.checked) {
                tile.classList.add('transparent');
            }

            map.appendChild(tile);
        }
    }

    function updateMapFromData(data) {

        let step = slider.value;
        let { date, mapData } = mapHistory[step];
        dateDisplay.textContent = date + " Game Minute: " + step;

        const teams = ['RED', 'BLUE', 'GREEN', 'YELLOW'];
        for (let i = 0; i < teams.length; i++) {
            if (document.getElementById(`teamPoints_${teams[i]}`)) {
                let suffix = " (+ 0/minute)";
                if (data.teamPointsGain[i] >= 0) {
                    suffix = " (+ " + data.teamPointsGain[i] + "/minute)";
                }
                document.getElementById(`teamPoints_${teams[i]}`).innerText = data.teamPoints[i].toLocaleString() + suffix;
            }

            if (document.getElementById(`teamNameAndConnected_${teams[i]}`)) {
                document.getElementById(`teamNameAndConnected_${teams[i]}`).innerText = `${teams[i]} (${data.teamConnectedPlayers[i].toLocaleString()})`;
            }

            if (document.getElementById(`teamPointsGain_${teams[i]}`)) {
                document.getElementById(`teamPointsGain_${teams[i]}`).innerText = data.teamPointsGain[i].toLocaleString();
            }
            if (document.getElementById(`teamSentSoldiers_${teams[i]}`)) {
                document.getElementById(`teamSentSoldiers_${teams[i]}`).innerText = data.teamSentSoldiers[i].toLocaleString();
            }
            if (document.getElementById(`teamSoldierGainMinute_${teams[i]}`)) {
                document.getElementById(`teamSoldierGainMinute_${teams[i]}`).innerText = data.teamSoldierGainMinute[i].toLocaleString() + ' /Last Minute';
            }
            if (document.getElementById(`teamSoldierGainHour_${teams[i]}`)) {
                document.getElementById(`teamSoldierGainHour_${teams[i]}`).innerText = data.teamSoldierGainHour[i].toLocaleString() + ' /Last Hour';
            }
            if (document.getElementById(`teamSentWorkers_${teams[i]}`)) {
                document.getElementById(`teamSentWorkers_${teams[i]}`).innerText = data.teamSentWorkers[i].toLocaleString();
            }
            if (document.getElementById(`teamWorkersGainMinute_${teams[i]}`)) {
                document.getElementById(`teamWorkersGainMinute_${teams[i]}`).innerText = data.teamWorkersGainMinute[i].toLocaleString() + ' /Last Minute';
            }
            if (document.getElementById(`teamWorkersGainHour_${teams[i]}`)) {
                document.getElementById(`teamWorkersGainHour_${teams[i]}`).innerText = data.teamWorkersGainHour[i].toLocaleString() + ' /Last Hour';
            }
            if (document.getElementById(`teamAverageHQ_${teams[i]}`)) {
                document.getElementById(`teamAverageHQ_${teams[i]}`).innerText = data.teamAverageHQ[i];
            }
            if (document.getElementById(`teamHQOver10_${teams[i]}`)) {
                document.getElementById(`teamHQOver10_${teams[i]}`).innerText = data.teamHQOver10[i];
            }
            if (document.getElementById(`teamHQOver15_${teams[i]}`)) {
                document.getElementById(`teamHQOver15_${teams[i]}`).innerText = data.teamHQOver15[i];
            }
            if (document.getElementById(`timeTillVPWin_${teams[i]}`)) {
                if(data.teamPointsGain[i] == 0) {
                    document.getElementById(`timeTillVPWin_${teams[i]}`).innerText = 'Infinite - Not gaining points';
                } else {
                    if (typeof gameWinGoal === 'undefined') {
                        document.getElementById(`timeTillVPWin_${teams[i]}`).innerText = 'Infinite - Not gaining points';
                    }
                    else{
                        let minutesTillWin = gameWinGoal / data.teamPointsGain[i];
						let hours = Math.floor(minutesTillWin / 60);
						let minutes = Math.floor(minutesTillWin % 60);
						document.getElementById(`timeTillVPWin_${teams[i]}`).innerText = `${hours} hours, ${minutes} minutes`;
                    }
                }
            }
        }

        if (step == slider.max && !gameActive) {
            document.getElementById('status').innerText = 'Live!';
        }
        else{
            document.getElementById('status').innerText = '';
        }

        // Clear existing tiles
        const existingTiles = document.querySelectorAll('.tile');
        existingTiles.forEach(tile => tile.remove());

        // Calculate tile size based on the actual image dimensions
        const tileSizeX = backgroundImage.clientWidth / mapWidth;
        const tileSizeY = backgroundImage.clientHeight / mapHeight;

        // Create new tiles
        for (let i = 0; i < mapData.length; i++) {
            const tile = document.createElement('div');
            tile.className = 'tile';
            tile.style.width = `${tileSizeX}px`;
            tile.style.height = `${tileSizeY}px`;
            tile.style.left = `${(i % mapWidth) * tileSizeX}px`;
            tile.style.top = `${Math.floor(i / mapWidth) * tileSizeY}px`;

            switch (mapData[i]) {
                case '1':
                    tile.style.backgroundColor = 'rgba(255, 0, 0, 0.40)';
                    break;
                case '2':
                    tile.style.backgroundColor = 'rgba(0, 0, 255, 0.40)';
                    break;
                case '3':
                    tile.style.backgroundColor = 'rgba(0, 255, 0, 0.40)';
                    break;
                case '4':
                    tile.style.backgroundColor = 'rgba(255, 255, 0, 0.40)';
                    break;
                case '5':
                    tile.style.backgroundColor = 'rgba(225, 0, 0, 0.25)';
                    break;
                case '6':
                    tile.style.backgroundColor = 'rgba(0, 0, 225, 0.25)';
                    break;
                case '7':
                    tile.style.backgroundColor = 'rgba(0, 225, 0, 0.25)';
                    break;
                case '8':
                    tile.style.backgroundColor = 'rgba(225, 225, 0, 0.25)';
                    break;
                case 'N':
                    tile.style.backgroundColor = 'rgba(225, 225, 255, 0.25)';
                    break;
                default:
                    tile.style.backgroundColor = 'transparent';
            }

            if (!toggleGrid.checked) {
                tile.classList.add('transparent');
            }

            map.appendChild(tile);
        }
    }

    
    function fetchLatestData() {
        if(gameActive){
            return;
        }
        const gamePrefix = "<?php echo $selectedGamePrefix; ?>";
        const gameNumber = "<?php echo $selectedGameNumber; ?>";
        let latestData = { date: '1' };
        fetch(`FetchLatestData.php?gamePrefix=${gamePrefix}&gameNumber=${gameNumber}`)
            .then(response => response.json())
            .then(latestData => {
                if (latestData.error) {
                    console.error(latestData.error);
                    return;
                }
                if (mapHistory && mapHistory[mapHistory.length - 1] && mapHistory[mapHistory.length - 1].date) {
                    lastDate = mapHistory[mapHistory.length - 1].date;
                    if (lastDate.toLocaleString() !== latestData.date.toLocaleString()) {
                        mapHistory.push(latestData);
                        slider.max = mapHistory.length - 1;
                        if (slider.value == slider.max - 1) {
                            currentIndex++;
                            slider.value = slider.max;
                            updateMapFromData(latestData);
                        }
                    }
                } else {
                    console.error("mapHistory, mapHistory[step], or mapHistory[step].date is undefined or null.");
                }
            })
            .catch(error => console.error('Error fetching latest data: ' + lastDate.toLocaleString() + ' vs ' + latestData.date.toLocaleString() + ' ::', error));
    }

    function updateGame() {
        const gameSelect = document.getElementById('gameNumber');
        const selectedGame = gameSelect.value.split('_');
        const gamePrefix = selectedGame[0];
        const gameNumber = selectedGame[1];
        const newUrl = `TimeLapse.php?gamePrefix=${gamePrefix}&gameNumber=${gameNumber}`;
        window.history.pushState({ path: newUrl }, '', newUrl);
        window.location.href = newUrl;
    }

    function updateAutoplay() {
        const autoplaySelect = document.getElementById('autoplaySelect');
        const speed = autoplaySelect.value;

        if (autoplayInterval) {
            clearInterval(autoplayInterval);
            autoplayInterval = null;
        }

        if(slider.value == slider.max){
            slider.value = 0;
        }

        if (speed !== 'off') {
            const interval = 1000 / parseInt(speed.replace('x', ''));
            console.log(`Autoplay speed: ${speed}, Interval: ${interval}ms`);
            autoplayInterval = setInterval(() => {
                if (parseInt(slider.value) < parseInt(slider.max)) {
                    slider.value = parseInt(slider.value) + 1;
                    updateMap(slider.value);
                } else {
                    clearInterval(autoplayInterval);
                    autoplayInterval = null;
                    console.log('Autoplay stopped: reached max value');
                }
            }, interval);
        }
    }

    let buildingsMaxShowLevel = <?php echo $buildingsMaxShowLevel; ?>;
    const buildings = <?php echo json_encode($buildings); ?>;
    const gameHQCostScaling = <?php echo json_encode($gameHQCostScaling); ?>;
    const gameBuildingCostScaling = <?php echo json_encode($gameBuildingCostScaling); ?>;

    function showMoreLevels() {
        buildingsMaxShowLevel += 5;
        const table = document.getElementById('buildingsTable');

        for (let level = buildingsMaxShowLevel - 4; level <= buildingsMaxShowLevel; level++) {
            const row = document.createElement('tr');
            row.className = level % 2 == 1 ? 'odd-row' : '';
            const levelCell = document.createElement('td');
            levelCell.className = 'sticky-column';
            levelCell.textContent = level;
            row.appendChild(levelCell);

            buildings.forEach(building => {
                let woodCost, ironCost, workerCost;
                if (building.name === 'HQ') {
                    woodCost = level >= building.woodStart ? building.woodCost * Math.pow(gameHQCostScaling[0], level - 1) : 0;
                    ironCost = level >= building.ironStart ? building.ironCost * Math.pow(gameHQCostScaling[1], level - 1) : 0;
                    workerCost = level >= building.workerStart ? building.workerCost * Math.pow(gameHQCostScaling[2], level - 1) : 0;
                } else {
                    woodCost = level >= building.woodStart ? building.woodCost * Math.pow(gameBuildingCostScaling[0], level - 1) : 0;
                    ironCost = level >= building.ironStart ? building.ironCost * Math.pow(gameBuildingCostScaling[1], level - 1) : 0;
                    workerCost = level >= building.workerStart ? building.workerCost * Math.pow(gameBuildingCostScaling[2], level - 1) : 0;
                }

                const woodCell = document.createElement('td');
                woodCell.className = 'wood-column';
                woodCell.textContent = Math.round(woodCost).toLocaleString();
                row.appendChild(woodCell);

                const ironCell = document.createElement('td');
                ironCell.className = 'iron-column';
                ironCell.textContent = Math.round(ironCost).toLocaleString();
                row.appendChild(ironCell);

                const workerCell = document.createElement('td');
                workerCell.className = 'worker-column';
                workerCell.textContent = Math.round(workerCost).toLocaleString();
                row.appendChild(workerCell);
            });

            table.appendChild(row);
        }
    }

    function showSection(section) {
        document.getElementById('overview').classList.remove('active');
        document.getElementById('buildings').classList.remove('active');
        document.getElementById(section).classList.add('active');
    }
    // Fetch the latest data every minute
    setInterval(fetchLatestData, 60000);

    if (selectedStepNumber !== -1) {
        slider.value = selectedStepNumber;
        // Remove the &minute=# parameter from the URL
        var url = new URL(window.location.href);
        url.searchParams.delete('minute');
        history.replaceState(null, '', url.toString());
    }

    slider.addEventListener('input', () => updateMap(slider.value));
    updateMap(slider.value);
</script>

</body>
</html>