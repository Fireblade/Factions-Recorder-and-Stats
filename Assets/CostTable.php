<?php
header("Cache-Control: no-store, no-cache, must-revalidate, max-age=0");
header("Cache-Control: post-check=0, pre-check=0", false);
header("Pragma: no-cache");

// Load configuration
$config = require 'config.php';

$servername = $config['servername'];
$username = $config['username'];
$password = $config['password'];
$dbname = $config['dbname'];


// Create connection
$conn = new mysqli($servername, $username, $password, $dbname);

// Check connection
if ($conn->connect_error) {
    die("Connection failed: " . $conn->connect_error);
}

$selectedGameNumber = isset($_GET['gameID']) ? $_GET['gameID'] : -1;

if ($selectedGameNumber != -1) {
    $sql = "SELECT * FROM ActiveGames WHERE GameID = ?";
    $stmt = $conn->prepare($sql);
    $stmt->bind_param("i", $selectedGameNumber);
} else {
    $sql = "SELECT * FROM ActiveGames ORDER BY GameID DESC LIMIT 1";
    $stmt = $conn->prepare($sql);
}

$stmt->execute();
$result = $stmt->get_result();


if ($result->num_rows > 0) {
    $row = $result->fetch_assoc();
    $selectedGameNumber = (int)$row['GameID'];
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

$buildingsMaxShowLevel = 25;

// Read the building data file
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
} else {
    die("Building data file not found.");
}
?>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Building Cost Table</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            background-color: #1a1a1a; /* Dark background */
            color: white; /* White text */
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 20px;
        }
        th, td {
            padding: 8px;
            text-align: left;
            border: 1px solid white; /* White border */
        }
        th {
            background-color: #333; /* Dark background */
        }
        .odd-row {
            background-color: #333; /* Slightly darker background for odd rows */
        }
        .wood-column {
            background-color: #5a2e0d; /* Darker brown for wood columns */
        }
        .iron-column {
            background-color: #2c4a6b; /* Darker blue for iron columns */
        }
        .worker-column {
            background-color: #8B8000; /* Dark yellow for worker columns */
        }
        .sticky-column {
            position: -webkit-sticky; /* For Safari */
            position: sticky;
            left: 0;
            background-color: #1a1a1a; /* Dark background */
            z-index: 1; /* Ensure it stays on top of other content */
        }
    </style>
</head>
<body>
    <h1>Building Cost Table - Game ID: <?php echo htmlspecialchars($selectedGameNumber); ?></h1>
    <table id="buildingsTable">
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
                        $woodCost = $level-1 >= $building['woodStart'] ? $building['woodCost'] * pow($gameHQCostScaling[0], $level - 2) : 0;
                        $ironCost = $level-1 >= $building['ironStart'] ? $building['ironCost'] * pow($gameHQCostScaling[1], $level - 2) : 0;
                        $workerCost = $level-1 >= $building['workerStart'] ? $building['workerCost'] * pow($gameHQCostScaling[2], $level - 2) : 0;
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

    <script>
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
                        if(level==1){
                            woodCost = 0;
                            ironCost = 0;
                            workerCost = 0;
                        }
                        else{
                            woodCost = level-1 >= building.woodStart ? building.woodCost * Math.pow(gameHQCostScaling[0], level - 2) : 0;
                            ironCost = level-1 >= building.ironStart ? building.ironCost * Math.pow(gameHQCostScaling[1], level - 2) : 0;
                            workerCost = level-1 >= building.workerStart ? building.workerCost * Math.pow(gameHQCostScaling[2], level - 2) : 0;
                        }
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
    </script>
</body>
</html>

