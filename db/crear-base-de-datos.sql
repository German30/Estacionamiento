-- Crea la base de datos de la aplicación.
-- Las tablas NO se crean aquí: las genera Entity Framework Core al arrancar la aplicación
-- aplicando las migraciones de src/Estacionamiento.Infraestructura/Persistencia/Migraciones.
--
-- Uso:
--   mysql --user=root --password < db/crear-base-de-datos.sql

CREATE DATABASE IF NOT EXISTS estacionamiento
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_0900_ai_ci;

-- Usuario dedicado, como alternativa a conectarse con root.
-- Descomentar y ajustar la contraseña antes de usar en un equipo compartido.
--
-- CREATE USER IF NOT EXISTS 'estacionamiento'@'localhost' IDENTIFIED BY 'cambiar-esta-contrasena';
-- GRANT ALL PRIVILEGES ON estacionamiento.* TO 'estacionamiento'@'localhost';
-- FLUSH PRIVILEGES;
