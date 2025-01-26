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
$tableName = "Game" . $gameID . "Leaderboards";

// Iterate through each player data
foreach ($data as $player) {
    $playerID = $player['PlayerID'];
    $scoreBlocks = $player['ScoreBlocks'];

    // Check if playerID exists in the database
    $checkQuery = "SELECT COUNT(*) FROM $tableName WHERE PlayerID = ?";
    $checkStmt = $conn->prepare($checkQuery);
    if ($checkStmt === false) {
        logDebug("Prepare failed: " . $conn->error);
        die("Prepare failed: " . $conn->error);
    }
    $checkStmt->bind_param("i", $playerID);
    $checkStmt->execute();
    $checkStmt->bind_result($count);
    $checkStmt->fetch();
    $checkStmt->close();

    // If playerID does not exist, skip to the next player
    if ($count == 0) {
        logDebug("PlayerID $playerID not found, skipping.");
        continue;
    }

    // Insert or update player data
    $query = "INSERT INTO $tableName (PlayerID, ScoreBlocks)
              VALUES (?, ?)
              ON DUPLICATE KEY UPDATE
              ScoreBlocks = VALUES(ScoreBlocks)";
    $stmt = $conn->prepare($query);
    if ($stmt === false) {
        logDebug("Prepare failed: " . $conn->error);
        die("Prepare failed: " . $conn->error);
    }
    $stmt->bind_param("is", $playerID, $scoreBlocks);
    if ($stmt->execute() === false) {
        die("Execute failed: " . $stmt->error);
    }
}
}

// Close connection
$conn->close();
logDebug("Connection closed");
?>
