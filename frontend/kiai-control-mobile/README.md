# Kiai Control Mobile

Aplicativo Flutter nativo para alunos consultarem aulas e acompanharem metas, e para professores fazerem a chamada e validarem presencas. O painel administrativo continua no frontend Blazor.

A troca de senha exige no mínimo 6 caracteres, uma letra maiúscula, uma minúscula e um caractere especial, conforme a política da API. `lib/src/password_policy.dart` centraliza a validação e a mensagem do app. Os testes cobrem também letras acentuadas, símbolos Unicode e a contagem mínima de caracteres.

## Requisitos

- Flutter SDK com suporte a Dart 3.4 ou superior.
- API Kiai Control em execucao e acessivel pelo dispositivo.
- Android: Android Studio com Android SDK, platform-tools e um emulador configurado, ou um dispositivo Android com depuracao USB.
- iOS: macOS com Xcode completo, ferramentas de linha de comando configuradas e um simulador ou dispositivo iOS.

As pastas nativas `android/` e `ios/` ja fazem parte deste projeto.

## Baixar dependencias e listar dispositivos

Na raiz deste projeto:

```bash
flutter pub get
flutter devices
```

Inicie um emulador pelo Android Studio ou pelo Xcode antes de executar o app.

## Executar no Android

Com a API local em `http://localhost:5210`, o emulador Android padrao acessa o computador host por `10.0.2.2`:

```bash
flutter run -d <id-do-dispositivo> \
  --dart-define=API_BASE_URL=http://10.0.2.2:5210/api/v1
```

O trafego HTTP sem TLS esta habilitado apenas no manifest de debug Android. Builds de release devem usar HTTPS.

## Executar no iOS

Com a API local em `http://localhost:5210`, o simulador iOS pode usar:

```bash
flutter run -d <id-do-simulador> \
  --dart-define=API_BASE_URL=http://127.0.0.1:5210/api/v1
```

O `Info.plist` permite conexoes locais para desenvolvimento, sem liberar HTTP arbitrario. Para dispositivo fisico, configure uma URL HTTPS acessivel pela rede do aparelho. Builds de distribuicao tambem devem usar HTTPS.

## Builds

```bash
flutter build apk --dart-define=API_BASE_URL=https://api.example.com/api/v1
flutter build ios --release --dart-define=API_BASE_URL=https://api.example.com/api/v1
```

O build iOS deve ser feito em macOS com Xcode completo. Para publicar, configure a assinatura/certificados de distribuicao e substitua `com.kiaicontrol.kiai_control_mobile` por um identificador de pacote registrado para o proprietario do app.

## Funcionalidades

- Login e persistencia de tokens com armazenamento seguro do sistema operacional.
- Consulta do perfil do proprio aluno.
- Listagem de aulas das modalidades vinculadas e check-in/desmarcacao ate o inicio da aula.
- Chamada do professor nas proprias aulas, com validacao de presenca, atraso e faltas.
- Consulta de metas e evolucao fitness.
- Troca de senha e encerramento de sessao.

As telas sao selecionadas pelo perfil autenticado (aluno ou professor). As consultas pessoais usam os endpoints `GET /api/v1/mobile/*`; o servidor resolve a pessoa e organizacao pelas claims autenticadas. A chamada do professor e restrita as aulas vinculadas ao professor autenticado.
