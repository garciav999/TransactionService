# Application Tests

Pruebas unitarias para la capa de aplicación del Transaction Service usando xUnit, Moq y FluentAssertions.

## 📊 Resumen de Pruebas

- **Total de Pruebas**: 29
- **Framework**: xUnit 2.9.2
- **Mocking**: Moq 4.20.72
- **Assertions**: FluentAssertions 8.8.0
- **Cobertura**: Coverlet

## 🧪 Clases Probadas

### TransactionCommands (8 tests)
- ✅ Creación de transacciones con datos válidos
- ✅ Creación con estado personalizado
- ✅ Validación de valores inválidos (≤ 0)
- ✅ Validación de SourceAccountId vacío
- ✅ Validación de TargetAccountId vacío
- ✅ Generación de IDs únicos
- ✅ Manejo de errores del repositorio

### TransactionService (10 tests)
- ✅ Actualización de estado con datos válidos
- ✅ Actualización con razón de rechazo
- ✅ Manejo de diferentes formatos de texto (case-insensitive)
- ✅ Validación de transacción no encontrada
- ✅ Validación de estados inválidos
- ✅ Propagación de excepciones del repositorio
- ✅ Estado Approved sin razón requerida
- ✅ Estado Rejected con razón

## 🚀 Ejecutar Pruebas

### Opción 1: Ejecución Simple
```powershell
dotnet test
```

### Opción 2: Con Cobertura de Código (Recomendado)
```powershell
.\run-tests-with-coverage.ps1
```

Este script ejecuta las pruebas y muestra:
- ✅ Resultados de todas las pruebas
- 📊 Porcentaje de cobertura de líneas
- 📊 Porcentaje de cobertura de ramas
- 📋 Detalles por clase

### Opción 3: Ejecución Manual con Cobertura
```powershell
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./TestResults/
```

## 📈 Cobertura Actual

```
Cobertura General:
- Líneas:  48.38%
- Ramas:   100%

Clases Core (100% cubierto):
✅ TransactionCommands         - 100%
✅ TransactionService          - 100%
✅ Transaction (Entity)        - 100%
✅ TransactionCreatedEvent     - 93.33%
✅ DomainEvent                 - 100%

Clases No Probadas (0% - No son parte de la lógica de negocio):
⚪ TransactionDto              - 0% (DTO sin lógica)
⚪ ResponseBuilder             - 0% (Helper de Lambda)
⚪ LambdaResponse              - 0% (DTO de respuesta)
⚪ TransactionStatusEvent      - 0% (DTO de Kafka)
```

## 🎯 Casos de Prueba Destacados

### Validaciones de Negocio
```csharp
[Theory]
[InlineData(0)]
[InlineData(-1)]
[InlineData(-100.50)]
public async Task InsertAsync_WithInvalidValue_ShouldThrowArgumentException(decimal invalidValue)
```

### Manejo de Estados Case-Insensitive
```csharp
[Theory]
[InlineData("Pending")]
[InlineData("pending")]
[InlineData("PENDING")]
public async Task UpdateTransactionStatusAsync_WithDifferentCasing_ShouldHandleCaseInsensitively(string status)
```

### Verificación de Publicación de Eventos
```csharp
_mockEventPublisher.Verify(x => x.PublishAsync(
    It.Is<TransactionCreatedEvent>(e =>
        e.TransactionExternalId == result &&
        e.SourceAccountId == sourceAccountId &&
        e.Value == value
    ), default), Times.Once);
```

## 📦 Dependencias

```xml
<PackageReference Include="xunit" Version="2.9.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.0" />
<PackageReference Include="Moq" Version="4.20.72" />
<PackageReference Include="FluentAssertions" Version="8.8.0" />
<PackageReference Include="coverlet.collector" Version="6.0.4" />
<PackageReference Include="coverlet.msbuild" Version="6.0.4" />
```

## 🔍 Patrones de Prueba Utilizados

### Arrange-Act-Assert (AAA)
Todas las pruebas siguen el patrón AAA para claridad:
```csharp
// Arrange: Configuración
var sourceAccountId = Guid.NewGuid();
_mockRepository.Setup(...);

// Act: Ejecución
var result = await _sut.InsertAsync(...);

// Assert: Verificación
result.Should().NotBe(Guid.Empty);
_mockRepository.Verify(...);
```

### System Under Test (SUT)
Convención de nomenclatura clara:
```csharp
private readonly TransactionCommands _sut; // System Under Test
```

### Mocking con Moq
Simulación de dependencias:
```csharp
_mockRepository.Setup(x => x.AddAsync(It.IsAny<Transaction>()))
    .Returns(Task.CompletedTask);
```

### FluentAssertions
Aserciones expresivas y legibles:
```csharp
await act.Should().ThrowAsync<ArgumentException>()
    .WithMessage("Value must be greater than zero.*")
    .WithParameterName("value");
```

## 📝 Convenciones de Nomenclatura

- **Clases de Prueba**: `{ClaseProbada}Tests`
- **Métodos de Prueba**: `{Método}_{Escenario}_{ResultadoEsperado}`
- **Ejemplos**:
  - `InsertAsync_WithValidData_ShouldCreateTransactionAndPublishEvent`
  - `UpdateTransactionStatusAsync_WhenTransactionNotFound_ShouldThrowInvalidOperationException`

## 🛠️ Configuración de Coverage

El archivo `coverlet.runsettings` está configurado para:
- Formato de salida: Cobertura XML
- Recolección automática con xUnit
- Generación de reportes por clase

## ✨ Mejoras Futuras

- [ ] Agregar pruebas de integración
- [ ] Aumentar cobertura de DTOs si agregan lógica
- [ ] Agregar pruebas de rendimiento
- [ ] Implementar mutation testing
- [ ] Agregar benchmark tests

## 📚 Recursos

- [xUnit Documentation](https://xunit.net/)
- [Moq Quick Start](https://github.com/moq/moq4)
- [FluentAssertions](https://fluentassertions.com/)
- [Coverlet](https://github.com/coverlet-coverage/coverlet)
