<?php
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

// Check if the required parameters are set
if(isset($_POST['devMode'])){
    $DevMode = filter_var($_POST['devMode'], FILTER_VALIDATE_BOOLEAN);
} else {
    $DevMode = false;
}

if (isset($_POST['mapData']) && isset($_POST['gameID']) && isset($_POST['isTestGame'])) {
    // Retrieve the parameters
    $mapData = $_POST['mapData'];
    $gameID = intval($_POST['gameID']);
    $isTestGame = filter_var($_POST['isTestGame'], FILTER_VALIDATE_BOOLEAN);

    // Define the file paths
    if ($DevMode) {
        $filePrefix = "TimeLapse/DevGame_";
    } else {
        $filePrefix = $isTestGame ? "TimeLapse/TestGame_" : "TimeLapse/Game_";
    }
    $historyFilePath = $filePrefix . $gameID . "_MapHistory.txt";
    $latestFilePath = $filePrefix . $gameID . "_Latest.txt";

    // Ensure the history file exists, create if not
    if (!file_exists($historyFilePath)) {
        touch($historyFilePath);
    }

    // Open the history file in append mode
    $historyFile = fopen($historyFilePath, "a");

    if ($historyFile) {
        // Write the mapData to the history file as a new line
        fwrite($historyFile, PHP_EOL . $mapData);

        // Close the history file
        fclose($historyFile);

        // Ensure the latest file exists, create if not
        if (!file_exists($latestFilePath)) {
            touch($latestFilePath);
        }

        // Open the latest file in write mode to overwrite it
        $latestFile = fopen($latestFilePath, "w");

        if ($latestFile) {
            // Write the mapData to the latest file
            fwrite($latestFile, $mapData);

            // Close the latest file
            fclose($latestFile);

            echo "Data successfully written to the files.";

            //Now increment the column GameMinute by 1 in table ActiveGames for the game.
            // Create connection
            $conn = new mysqli($servername, $username, $password, $dbname);

            // Check connection
            if ($conn->connect_error) {
				echo "Connection failed: " . $conn->connect_error;
				exit;
			}
            // Increment the GameMinute column by 1 for the specified game
			$sql = "UPDATE ActiveGames SET GameMinute = GameMinute + 1 WHERE GameID = ?";

            // Prepare and bind
            $stmt = $conn->prepare($sql);
            $stmt->bind_param("i", $gameID);

            if ($stmt->execute() === TRUE) {
				echo "Game minute incremented successfully.";
			} else {
				echo "Error updating game minute: " . $stmt->error;
			}

			// Close the statement and the database connection
			$stmt->close();
			$conn->close();

        } else {
            echo "Failed to open the latest file.";
        }
    } else {
        echo "Failed to open the history file.";
    }
} else {
    echo "Required parameters are missing.";
}

?>
