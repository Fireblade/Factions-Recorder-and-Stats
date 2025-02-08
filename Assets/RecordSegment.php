<?php
header("Cache-Control: no-store, no-cache, must-revalidate, max-age=0");
header("Cache-Control: post-check=0, pre-check=0", false);
header("Pragma: no-cache");

// Load configuration
$config = require 'config.php';

$storedApiKey = $config['api_key'];

// Check if the API key is provided and valid
if (!isset($_SERVER['HTTP_API_KEY']) || $_SERVER['HTTP_API_KEY'] !== $storedApiKey) {
    http_response_code(403);
    echo "Unauthorized access.";
    exit;
}

if (isset($_POST['gameID']) && isset($_POST['playerData']) && isset($_POST['gameMinute'])) {
    $gameID = intval($_POST['gameID']);
    $playerData = $_POST['playerData'];
    $gameMinute = intval($_POST['gameMinute']);
    echo "Parameters found";
} else {
    echo "Missing parameters";
    exit;
}

// Check if parameters are null
if ($gameID === null || $playerData === null) {
    die("Missing parameters: GameID or playerData is null");
}

// Decode JSON content
$data = json_decode($playerData, true);
if (json_last_error() !== JSON_ERROR_NONE) {
    die("JSON decode error: " . json_last_error_msg());
}

// Create directory if it doesn't exist
$directoryPath = "GameData/$gameID";
if (!is_dir($directoryPath)) {
    mkdir($directoryPath, 0777, true);
}

// Write JSON data to file
$filePath = "$directoryPath/$gameMinute.txt";
file_put_contents($filePath, $playerData);

echo "Data saved successfully.";
?>
