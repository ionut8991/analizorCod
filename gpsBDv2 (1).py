import serial
import pyodbc
from datetime import datetime

def connect_to_database():
    # Asigurați-vă că detaliile de conectare sunt corecte
    server = 'techallenge.database.windows.net'
    database = 'MonitFlota'
    username = 'tech_admin'
    password = 'Parola1234'
    driver = '{ODBC Driver 18 for SQL Server}'  # Asigurați-vă că aveți acest driver instalat

    connection_string = f"DRIVER={driver};SERVER={server};DATABASE={database};UID={username};PWD={password}"
    conn = pyodbc.connect(connection_string)
    return conn

def save_coordinates(conn, vehicle_id, coordinates, speed, potholes):
    """Salvează datele de localizare în tabela 'currentloc'."""
    cursor = conn.cursor()
    query = """INSERT INTO currentloc (vehicle_id, coordinates, speed, potholes) 
               VALUES (?, ?, ?, ?)"""
    cursor.execute(query, (vehicle_id, coordinates, speed, potholes))
    conn.commit()
    cursor.close()

def save_pothole_report(conn, latitude, longitude):
    """Salvează un raport despre gropi în tabela 'UserTrafficReports'."""
    cursor = conn.cursor()
    report_date = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    query = """INSERT INTO UserTrafficReports (Latitude, Longitude, Lanes, Potholes, RoadCondition, SurfaceType, TrafficLevel, Hazards, ReportDate, ReporterID) 
               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)"""
    # Valorile sunt cele specificate în cerință
    cursor.execute(query, (latitude, longitude, 1, 1, 'Poor', 'Asphalt', 'Moderate', 'Potholes', report_date, 1))
    conn.commit()
    cursor.close()
    print("A fost adăugat un raport de groapă în UserTrafficReports.")

def main():
    print("Se pornește cititorul de coordonate GPS...")

    # Configurați portul serial
    port = "/dev/ttyUSB0"  # Schimbați cu portul dumneavoastră
    baudrate = 115200
    conn = None  # Inițializați conn cu None

    try:
        conn = connect_to_database()
        print("Conectat la baza de date.")

        with serial.Serial(port, baudrate, timeout=1) as ser:
            print("Se ascultă datele GPS...")

            while True:
                data_line = ser.readline()

                try:
                    decoded_line = data_line.decode('utf-8', errors='ignore').strip()

                    if "Longitude, Latitude" in decoded_line:
                        latitude_line = ser.readline().decode('utf-8', errors='ignore').strip()
                        longitude_line = ser.readline().decode('utf-8', errors='ignore').strip()

                        try:
                            latitude = float(latitude_line)
                            longitude = float(longitude_line)
                            coordinates = f"{latitude},{longitude}"

                            ser.readline()  # Sare peste eticheta "Speed (km/h):"
                            speed_line = ser.readline().decode('utf-8', errors='ignore').strip()
                            speed = float(speed_line) if speed_line else 0.0

                            ser.readline()  # Sare peste eticheta "Potholes:"
                            potholes_line = ser.readline().decode('utf-8', errors='ignore').strip()
                            potholes = int(potholes_line) if potholes_line else 0

                            coordinatesPrint = f"{coordinates}, Viteză: {speed}, Gropi: {potholes}"
                            print(f"Coordonate: {coordinatesPrint}")

                            # Salvare în baza de date
                            save_coordinates(conn, vehicle_id=1, coordinates=coordinates, speed=speed, potholes=potholes)

                            # Dacă a fost detectată o groapă, se adaugă raportul
                            if potholes > 0:
                                save_pothole_report(conn, latitude, longitude)

                        except ValueError as e:
                            print(f"Format de date invalid pentru latitudine, longitudine, viteză sau gropi: {e}")
                except UnicodeDecodeError as e:
                    print(f"Eroare de decodare: {e}")

    except Exception as e:
        print(f"Eroare: {e}")
    finally:
        if conn:
            conn.close()
            print("Conexiunea la baza de date a fost închisă.")

if __name__ == "__main__":
    main()
