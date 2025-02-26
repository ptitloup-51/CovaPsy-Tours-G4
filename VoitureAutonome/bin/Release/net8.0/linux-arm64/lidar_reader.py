from rplidar import RPLidar
import numpy as np
import time
from multiprocessing import Manager, Process

def lidar_scan(shared_data):
    """ Fonction exécutée en arrière-plan pour collecter les données du LiDAR """
    lidar = RPLidar("/dev/ttyUSB0", baudrate=256000)
    lidar.connect()
    lidar.start_motor()
    time.sleep(1)

    try:
        for scan in lidar.iter_scans(scan_type='express'):
            for i in range(len(scan)):
                angle = min(359, max(0, 359 - int(scan[i][1])))  # Angle entre 0 et 359°
                shared_data[angle] = scan[i][2]  # Distance en mm

    except KeyboardInterrupt:
        print("Arrêt du LiDAR")
    
    finally:
        lidar.stop_motor()
        lidar.stop()
        lidar.disconnect()

def start_lidar():
    """ Fonction principale pour exécuter le processus de collecte en arrière-plan """
    manager = Manager()
    shared_data = manager.list([0] * 360)  # Tableau partagé accessible depuis C#

    process = Process(target=lidar_scan, args=(shared_data,))
    process.daemon = True  # Permet l'arrêt avec le programme principal
    process.start()

    return shared_data

if __name__ == "__main__":
    data = start_lidar()
    while True:
        print("Angle 10° - Distance :", data[10], "mm")  # Exemple d'affichage
        time.sleep(1)