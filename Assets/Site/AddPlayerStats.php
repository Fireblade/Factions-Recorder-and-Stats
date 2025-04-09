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
$storedApiKey = $config['api_key'];

// Check if the API key is provided and valid
if (!isset($_SERVER['HTTP_API_KEY']) || $_SERVER['HTTP_API_KEY'] !== $storedApiKey) {
    http_response_code(403);
    echo "Unauthorized access.";
    exit;
}

if (isset($_POST['gameID']) && isset($_POST['playerData'])) {
    $gameID = intval($_POST['gameID']);
    $playerData = $_POST['playerData'];
    echo "Parameters found";
} else {
    echo "Missing parameters";
    exit;
}

// Function to log debug messages
function logDebug($message) {
    //$logFile = 'updaterDebug.txt';
    //$timestamp = date("Y-m-d H:i:s");
    //file_put_contents($logFile, "[$timestamp] $message\n", FILE_APPEND);
}

// Create connection
$conn = new mysqli($servername, $username, $password, $dbname);

// Check connection
if ($conn->connect_error) {
    logDebug("Connection failed: " . $conn->connect_error);
    die("Connection failed: " . $conn->connect_error);
}

// Check if parameters are null
if ($gameID === null || $playerData === null) {
    logDebug("Missing parameters: GameID or playerData is null");
    die("Missing parameters: GameID or playerData is null");
}

// Decode JSON content
$data = json_decode($playerData, true);
if (json_last_error() !== JSON_ERROR_NONE) {
    logDebug("JSON decode error: " . json_last_error_msg());
    die("JSON decode error: " . json_last_error_msg());
}

logDebug("JSON decoded successfully");

// Table name
$tableName = "Players";

// Iterate through each player data
foreach ($data as $player) {
    $playerID = $player['PlayerID'];
    $playerName = $player['PlayerName'];
    $hqLevel = $player['HQLevel'];
    $soldiers = $player['Soldiers'];
    $workers = $player['Workers'];
    $totalCasesCaptured = $player['TilesCaptured'];
    $team = $player['Team'];
    $rankBySoldiers = $player['RankBySoldiers'];
    $rankByWorkers = $player['RankByWorkers'];
    $rankByHQLevel = $player['RankByHQLevel'];
    $awarded = $player['Awarded'];

    // Determine the team and set the corresponding column
    $playedRed = $team === 'RED' ? 1 : 0;
    $playedGreen = $team === 'GREEN' ? 1 : 0;
    $playedBlue = $team === 'BLUE' ? 1 : 0;
    $playedYellow = $team === 'YELLOW' ? 1 : 0;

    // Retrieve the player's current name
    $query = "SELECT PlayerName FROM $tableName WHERE PlayerID = ?";
    $stmt = $conn->prepare($query);
    if ($stmt === false) {
        logDebug("Prepare failed: " . $conn->error);
        die("Prepare failed: " . $conn->error);
    }
    $stmt->bind_param("i", $playerID);
    if ($stmt->execute() === false) {
        die("Execute failed: " . $stmt->error);
    }
    $stmt->bind_result($currentName);
    $stmt->fetch();
    $stmt->close();

    // If the player's current name is different from the new name, add the current name to the previous names table
    if ($currentName !== $playerName) {
        $query = "INSERT INTO PlayerPreviousNames (PlayerID, PreviousName) VALUES (?, ?)
                  ON DUPLICATE KEY UPDATE PreviousName = PreviousName";
        $stmt = $conn->prepare($query);
        if ($stmt === false) {
            logDebug("Prepare failed: " . $conn->error);
            die("Prepare failed: " . $conn->error);
        }
        $stmt->bind_param("is", $playerID, $currentName);
        if ($stmt->execute() === false) {
            die("Execute failed: " . $stmt->error);
        }
        $stmt->close();
    }

    // Insert or update player data
    $query = "INSERT INTO $tableName (PlayerID, PlayerName, TotalHQLevel, TotalSoldiers, TotalWorkers, TotalCasesCaptured, TotalSoldierRank, TotalWorkerRank, TotalHQRank, Awards, PlayedRed, PlayedGreen, PlayedBlue, PlayedYellow)
              VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
              ON DUPLICATE KEY UPDATE
              GamesPlayed = GamesPlayed + 1,
              TotalHQLevel = TotalHQLevel + VALUES(TotalHQLevel),
              TotalSoldiers = TotalSoldiers + VALUES(TotalSoldiers),
              TotalWorkers = TotalWorkers + VALUES(TotalWorkers),
              TotalCasesCaptured = TotalCasesCaptured + VALUES(TotalCasesCaptured),
              TotalSoldierRank = TotalSoldierRank + VALUES(TotalSoldierRank),
              TotalWorkerRank = TotalWorkerRank + VALUES(TotalWorkerRank),
              TotalHQRank = TotalHQRank + VALUES(TotalHQRank),
              Awards = Awards + IF(VALUES(Awards) = 1, 1, 0),
              PlayedRed = PlayedRed + VALUES(PlayedRed),
              PlayedGreen = PlayedGreen + VALUES(PlayedGreen),
              PlayedBlue = PlayedBlue + VALUES(PlayedBlue),
              PlayedYellow = PlayedYellow + VALUES(PlayedYellow)";
    
    $stmt = $conn->prepare($query);
    if ($stmt === false) {
        logDebug("Prepare failed: " . $conn->error);
        die("Prepare failed: " . $conn->error);
    }
    $stmt->bind_param("isiiiiiiiiiiii", $playerID, $playerName, $hqLevel, $soldiers, $workers, $totalCasesCaptured, $rankBySoldiers, $rankByWorkers, $rankByHQLevel, $awarded, $playedRed, $playedGreen, $playedBlue, $playedYellow);
    if ($stmt->execute() === false) {
        die("Execute failed: " . $stmt->error);
    }
}

// Close connection
$conn->close();
?>
