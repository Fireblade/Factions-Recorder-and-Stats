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

// Function to display player profile
function displayPlayerProfile($conn, $userID) {
    $sql = "SELECT PlayerName, GamesPlayed, Awards, TotalCasesCaptured, TotalHQLevel, TotalSoldiers, TotalWorkers, PlayedRed, PlayedGreen, PlayedBlue, PlayedYellow FROM Players WHERE PlayerID = ?";
    $stmt = $conn->prepare($sql);
    $stmt->bind_param("i", $userID);
    $stmt->execute();
    $result = $stmt->get_result();

    if ($result->num_rows > 0) {
        // Fetch the player data
        $row = $result->fetch_assoc();
        $playerName = $row['PlayerName'];
        $gamesPlayed = $row['GamesPlayed'];
        $awards = $row['Awards'];
        $totalCasesCaptured = $row['TotalCasesCaptured'];
        $totalHQLevel = $row['TotalHQLevel'];
        $totalSoldiers = $row['TotalSoldiers'];
        $totalWorkers = $row['TotalWorkers'];
        $playedRed = $row['PlayedRed'];
        $playedGreen = $row['PlayedGreen'];
        $playedBlue = $row['PlayedBlue'];
        $playedYellow = $row['PlayedYellow'];

        // Calculate averages
        $avgHQLevel = $gamesPlayed > 0 ? $totalHQLevel / $gamesPlayed : 0;
        $avgSoldiers = $gamesPlayed > 0 ? $totalSoldiers / $gamesPlayed : 0;
        $avgWorkers = $gamesPlayed > 0 ? $totalWorkers / $gamesPlayed : 0;
        $avgCasesCaptured = $gamesPlayed > 0 ? $totalCasesCaptured / $gamesPlayed : 0;

        // Display player information
        echo "<div style='background-color: #8D6A3F; padding: 20px;'>";
        echo "<div style='border: 1px solid black; padding: 10px; margin-bottom: 10px; display: flex; justify-content: space-between; align-items: center;'>";
        echo "<h1>$playerName</h1>";
        echo "<button onclick='togglePreviousNames($userID)'>Previous Names</button>";
        echo "</div>";
        echo "<div style='border: 1px solid black; padding: 10px;'>";
        echo "<p>Games Played: $gamesPlayed, Awards: $awards</p>";
        echo "<p>Red: $playedRed, Blue: $playedBlue, Green: $playedGreen, Yellow: $playedYellow</p>";
        echo "</div>";
        echo "<div style='border: 1px solid black; padding: 10px;'>";
        echo "<p>Total Soldiers: $totalSoldiers, Total Workers: $totalWorkers, Total Cases Captured: $totalCasesCaptured</p>";
        echo "<p>Average HQ Level per Game: " . number_format($avgHQLevel, 2, '.', ',') . "</p>";
        echo "<p>Average Soldiers per Game: " . number_format($avgSoldiers, 0, '.', ',') . "</p>";
        echo "<p>Average Workers per Game: " . number_format($avgWorkers, 0, '.', ',') . "</p>";
        echo "<p>Average Cases Captured per Game: " . number_format($avgCasesCaptured, 0, '.', ',') . "</p>";
        echo "</div>";
        echo "</div>";
    } else {
        echo "No player found with the given ID.";
    }
}

// Check if userID is set
if (isset($_GET['userID'])) {
    $userID = intval($_GET['userID']);
    $extraContent = '<button onclick="viewPlayerList()">Player List</button>';
} else {
    header("Location: PlayerList.php");
    exit;
}
?>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Player Profile</title>
    <style>
        body {
            background-color: #8D6A3F;
            color: #000;
            font-family: Arial, sans-serif;
        }
        .content {
            padding: 20px;
        }
        .box {
            border: 1px solid black;
            padding: 10px;
            margin-bottom: 10px;
        }
        /* Navbar styles */
        #topBanner {
            background-color: #333;
            color: white;
            padding: 10px;
            text-align: center;
            display: flex;
            justify-content: space-between;
            align-items: center;
            position: relative;
        }
        #menuButton img {
            width: 30px; /* Set a fixed width for the image */
            height: 30px; /* Set a fixed height for the image */
        }
        #menuButton {
            background: #f0f0f0; /* Light gray background */
            border: 1px solid #ccc; /* Slightly darker border */
            padding: 5px; /* Add some padding */
            border-radius: 5px; /* Optional: rounded corners */
        }
        #menu {
            display: none;
            position: absolute;
            top: 50px;
            left: 10px;
            width: 200px;
            background-color: #333;
            border: 1px solid #ccc;
            padding: 10px;
            z-index: 1000;
        }
        #menu a {
            display: block;
            color: white;
            text-decoration: none;
            padding: 5px 0;
        }
        #menu a:hover {
            background-color: #444;
        }
        #menu .section-label {
            margin-top: 10px;
            font-weight: bold;
            color: #ccc;
        }
        /* Previous names container styles */
        #previousNamesContainer {
            display: none;
            position: fixed;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            background-color: #8D6A3F;
            border: 1px solid black;
            padding: 20px;
            z-index: 1001;
        }
        #previousNamesContainer h2 {
            margin-top: 0;
        }
        #previousNamesContainer button {
            position: absolute;
            top: 10px;
            right: 10px;
        }
    </style>
</head>
<body>
    <?php
    // Include the navigation bar
    include 'NavBar.php';
    renderNavbar(isset($_GET['hideMenu']) && $_GET['hideMenu'] === 'true', $extraContent);
    ?>
    <div class="content">
        <?php
        // Display player profile
        if (isset($_GET['userID'])) {
            $userID = intval($_GET['userID']);
            displayPlayerProfile($conn, $userID);
        }
        ?>
    </div>
    <div id="previousNamesContainer">
        <button onclick="togglePreviousNames()">Close</button>
        <h2>Previous Names</h2>
        <ul id="previousNamesList"></ul>
    </div>
    <script>
        function viewPlayerList() {
            window.location.href = "PlayerList.php";
        }

        function toggleMenu() {
            const menu = document.getElementById('menu');
            if (menu.style.display === 'block') {
                menu.style.display = 'none';
            } else {
                menu.style.display = 'block';
            }
        }

        function updateGame() {
            const gameSelect = document.getElementById('gameNumber');
            const selectedGame = gameSelect.value.split(' ');
            const gameNumber = selectedGame[1];
            const newUrl = `TimeLapse.php?gameNumber=${gameNumber}`;
            window.history.pushState({ path: newUrl }, '', newUrl);
            window.location.href = newUrl;
        }

        function togglePreviousNames(userID) {
            const container = document.getElementById('previousNamesContainer');
            if (container.style.display === 'block') {
                container.style.display = 'none';
            } else {
                fetchPreviousNames(userID);
                container.style.display = 'block';
            }
        }

        function fetchPreviousNames(userID) {
            fetch(`fetchPreviousNames.php?userID=${userID}`)
                .then(response => response.json())
                .then(data => {
                    const list = document.getElementById('previousNamesList');
                    list.innerHTML = '';
                    data.forEach(name => {
                        const listItem = document.createElement('li');
                        listItem.textContent = name;
                        list.appendChild(listItem);
                    });
                })
                .catch(error => console.error('Error fetching previous names:', error));
        }
    </script>
</body>
</html>

<?php
// Close connection
$conn->close();
?>

