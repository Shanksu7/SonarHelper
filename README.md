# SonarHelper

Una aplicación de Windows Forms para gestionar y ejecutar análisis de SonarQube en múltiples proyectos.

## Requisitos Previos

- .NET 8.0 o superior
- SonarQube Scanner for .NET instalado
- SonarQube Server en ejecución (por defecto en localhost:9000)

## Instalación

1. Clone el repositorio
2. Abra la solución en Visual Studio
3. Compile y ejecute el proyecto

## Uso

1. Agregue un nuevo proyecto:
   - Nombre del proyecto (debe coincidir con la clave del proyecto en SonarQube)
   - Token de autenticación de SonarQube
   - Ruta del proyecto a analizar

2. Gestione proyectos existentes:
   - Seleccione un proyecto de la lista para ver/editar sus detalles
   - Actualice la información según sea necesario
   - Elimine proyectos que ya no necesite

3. Ejecute análisis:
   - Seleccione un proyecto de la lista
   - Asegúrese de que SonarQube esté en ejecución
   - Haga clic en "Ejecutar Análisis"

## Notas

- La aplicación verifica automáticamente si SonarQube está en ejecución antes de iniciar un análisis
- Los datos de los proyectos se almacenan localmente en un archivo JSON
- Se mantiene un registro de la última fecha de análisis para cada proyecto 
