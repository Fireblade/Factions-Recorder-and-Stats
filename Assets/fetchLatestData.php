<?php
header("Cache-Control: no-store, no-cache, must-revalidate, max-age=0");
header("Cache-Control: post-check=0, pre-check=0", false);
header("Pragma: no-cache");
header("Expires: 0");

$dir = 'TimeLapse';
$selectedGamePrefix = isset($_GET['gamePrefix']) ? $_GET['gamePrefix'] : die("Game prefix not specified.");
$selectedGameNumber = isset($_GET['gameNumber']) ? $_GET['gameNumber'] : die("Game number not specified.");

$latestDataFile = $dir . "/{$selectedGamePrefix}_{$selectedGameNumber}_Latest.txt";
header("ETag: \"" . md5(uniqid(rand(), true)) . "\"");
header("Last-Modified: " . gmdate("D, d M Y H:i:s", filemtime($latestDataFile)) . " GMT");

if (!file_exists($latestDataFile)) {
    die(json_encode(['error' => 'Latest data file not found.']));
}

$latestLine = file_get_contents($latestDataFile);
list($date, $mapData, $teamPoints, $teamPointsGain, $teamSentSoldiers, $teamSoldierGainMinute, $teamSoldierGainHour, $teamSentWorkers, $teamWorkersGainMinute, $teamWorkersGainHour, $teamAverageHQ, $teamHQOver10, $teamHQOver15, $teamConnectedPlayers) = explode('_', $latestLine);

$latestData = [
    'date' => $date,
    'mapData' => $mapData,
    'teamPoints' => array_map('intval', array_pad(explode('=', $teamPoints), 4, 0)),
    'teamPointsGain' => array_map('intval', array_pad(explode('=', $teamPointsGain), 4, 0)),
    'teamSentSoldiers' => array_map('intval', array_pad(explode('=', $teamSentSoldiers), 4, 0)),
    'teamSoldierGainMinute' => array_map(function($value) { return round(floatval($value), 2); }, array_pad(explode('=', $teamSoldierGainMinute), 4, 0)),
    'teamSoldierGainHour' => array_map(function($value) { return round(floatval($value), 2); }, array_pad(explode('=', $teamSoldierGainHour), 4, 0)),
    'teamSentWorkers' => array_map('intval', array_pad(explode('=', $teamSentWorkers), 4, 0)),
    'teamWorkersGainMinute' => array_map(function($value) { return round(floatval($value), 2); }, array_pad(explode('=', $teamWorkersGainMinute), 4, 0)),
    'teamWorkersGainHour' => array_map(function($value) { return round(floatval($value), 2); }, array_pad(explode('=', $teamWorkersGainHour), 4, 0)),
    'teamAverageHQ' => array_map(function($value) { return round(floatval($value), 2); }, array_pad(explode('=', $teamAverageHQ), 4, 0)),
    'teamHQOver10' => array_map('intval', array_pad(explode('=', $teamHQOver10), 4, 0)),
    'teamHQOver15' => array_map('intval', array_pad(explode('=', $teamHQOver15), 4, 0)),
    'teamConnectedPlayers' => array_map('intval', explode('=', $teamConnectedPlayers) + [0, 0, 0, 0])
];

echo json_encode($latestData);
?>