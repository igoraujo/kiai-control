# Kiai Control Mobile

Aplicativo Flutter para Android e iOS, integrado à API .NET existente. A navegação é montada com `GET /api/v1/mobile/context`: o servidor combina os vínculos ativos, papéis e permissões efetivas. O app aceita todos os papéis retornados, inclusive acessos sem módulos operacionais; não escolhe uma única função salva no dispositivo.

O código de agenda, chamada, metas e política de senha foi preservado. As duas estruturas de navegação limitadas a aluno/professor foram substituídas por uma navegação comum. `app.dart` mantém bootstrap e os fluxos existentes; `feature_views.dart` contém as telas ampliadas; `mobile_models.dart` e `models.dart` contêm os modelos; `api_client.dart` concentra transporte/sessão; `push_service.dart` integra o SDK de notificações. Os estados de tela ficam nos respectivos widgets. Não há dados de demonstração no aplicativo.

## Preparação

1. Instale Flutter/Dart compatíveis com o `pubspec.yaml` e os SDKs nativos.
2. Aplique as migrations da API em ordem, incluindo `017_mobile_accounts_and_push.sql`.
3. Inicie a API em uma URL acessível pelo aparelho.
4. Execute `flutter pub get`.

Android emulador:

```sh
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5210/api/v1
```

iOS simulador, em macOS com Xcode completo:

```sh
flutter run --dart-define=API_BASE_URL=http://127.0.0.1:5210/api/v1
```

A URL inclui `/api/v1`. HTTP local é permitido no Android debug e pelo transporte local do iOS. Distribuição exige `API_BASE_URL` HTTPS, identificador de pacote próprio e assinatura. Configure a assinatura Android de produção: a configuração anterior do projeto ainda usa a chave de debug. O código não contém credenciais de assinatura ou de serviços externos.

## Recursos e contratos

Consulte [MOBILE_API.md](../../MOBILE_API.md) para o mapeamento completo, novos contratos, semântica dos indicadores e limitações.

- Sessão segura no Keychain/armazenamento seguro Android; refresh com rotação compartilhado entre requisições, logout, expiração global e troca obrigatória de senha. Após trocar a senha, a API revoga as sessões e o app solicita novo login.
- Dashboard com dados reais de relatórios para os acessos autorizados, frequência para alunos, aulas para professores e notificações recentes.
- Agenda da semana da academia; check-in até o início da aula, sempre pendente. A API informa `canCheckIn`/`canCancelCheckIn` e verifica novamente na mutação.
- Validação/rejeição de check-ins conforme autorização e vínculo da aula. Pendente não é presença confirmada.
- Evolução: frequência mensal, totais, sequência, metas, histórico, conquistas, faixa/grau e aulas restantes para o critério de aulas do próximo nível.
- Ranking anual/total por aulas concluídas com presença validada. Empates compartilham posição; ocultar o perfil remove sua participação pública e posição.
- Edição pessoal, foto JPEG/PNG até 512 KB, privacidade do ranking e consentimento de push. E-mail de contato não altera o login.
- Notificações pessoais paginadas, conteúdo autenticado e leitura idempotente. Toques de push abrem os detalhes consultados novamente na API, impedindo abertura de dados de outro usuário.

As telas apresentam carregamento, ausência de dados, erro e nova tentativa. Datas de aulas são exibidas no fuso do aparelho; a semana e o ano dos indicadores são definidos pelo servidor no fuso IANA da academia. A confirmação do check-in compara instantes UTC, inclusive quando o cliente usa outro offset.

## Push Android e iOS

A integração usa Firebase Messaging 17 e Firebase Admin .NET 3.7, com o contrato atual de registro via `register`/`onRegistered`: o identificador de envio é o **Firebase Installation ID (FID)**, enviado no campo `token` do registro de dispositivos. Não confundir com a API legada de registration token. Atualizações do registro são enviadas à API e o logout remove o registro e chama `unregister` no SDK.

Por padrão `PUSH_ENABLED=false`; o restante do app funciona sem Firebase. Para ativar:

1. Crie um projeto Firebase e registre o Application ID Android e Bundle ID iOS definitivos. Habilite Cloud Messaging.
2. No iOS, ative Push Notifications e Background Modes/Remote notifications no App ID e no perfil de provisionamento, e cadastre sua chave APNs no Firebase. O projeto inclui o entitlement `aps-environment`, desenvolvimento no Debug e produção no Release/Profile; configure equipe e assinatura no Xcode. Não desative o swizzling do Firebase.
3. Crie um JSON **fora do repositório** com os valores públicos do aplicativo Firebase. O `FIREBASE_APP_ID` difere entre Android e iOS. Use um arquivo por plataforma:

```json
{
  "API_BASE_URL": "https://sua-api.example/api/v1",
  "PUSH_ENABLED": "true",
  "FIREBASE_API_KEY": "valor-do-aplicativo-firebase",
  "FIREBASE_APP_ID": "id-do-aplicativo-android-ou-ios",
  "FIREBASE_SENDER_ID": "numero-do-projeto",
  "FIREBASE_PROJECT_ID": "id-do-projeto",
  "FIREBASE_IOS_BUNDLE_ID": "identificador-ios"
}
```

```sh
flutter run --dart-define-from-file=/caminho/privado/mobile-android.json
```

A inicialização recebe `FirebaseOptions` explicitamente, sem arquivos de conta de serviço no cliente. O Android cria o canal `kiai_notifications` e declara a permissão do Android 13+. O SDK não registra automaticamente antes do consentimento. O usuário ativa a opção no perfil e salva para solicitar a permissão do sistema; se já negou, precisa permitir nas configurações do aparelho. O iOS precisa obter o token APNs antes do registro FCM.

4. No servidor, configure `Push__Enabled=true` e Application Default Credentials com permissão de envio FCM. Por exemplo, `GOOGLE_APPLICATION_CREDENTIALS` pode apontar para um arquivo de conta de serviço **fora do repositório**, acessível apenas ao processo da API. Prefira identidade de workload em produção. O projeto Firebase das credenciais deve corresponder ao do aplicativo.
5. Crie uma notificação pelo endpoint existente `POST /notifications` com `channel: "push"`. A pessoa precisa ter `pushEnabled=true` e dispositivo registrado. A fila respeita `scheduledAt`, utiliza lease e até cinco tentativas por dispositivo. `sent` significa aceitação pelo provedor, sem garantir entrega ao aparelho. `read` é independente da entrega. Preferências são verificadas novamente na consulta da fila.

Primeiro plano: aviso no app com ação Abrir. Segundo plano: sistema operacional apresenta a notificação e `onMessageOpenedApp` abre o conteúdo. App fechado: `getInitialMessage` recupera o toque após login; o backend verifica a propriedade da notificação. O handler de background é uma função de entrada preservada no build. Force stop e restrições de bateria/permissões dos sistemas podem impedir entrega.

Referências oficiais: [recebimento Flutter](https://firebase.google.com/docs/cloud-messaging/flutter/receive-messages), [configuração Firebase Admin](https://firebase.google.com/docs/admin/setup).

## Validação

```sh
flutter analyze --no-pub
flutter test --no-pub
flutter build apk --debug --dart-define=API_BASE_URL=https://sua-api.example/api/v1
flutter build ios --release --no-codesign --dart-define=API_BASE_URL=https://sua-api.example/api/v1
```

Os testes do cliente HTTP usam respostas controladas somente em `test/` para verificar rotação concorrente, expiração, conexão, contratos inválidos e ausência de sucesso falso. Os testes de widgets verificam navegação por capacidades, perfis novos, pendências e retorno ao login. Os testes da API usam PostgreSQL temporário e incluem requisições HTTP reais; Docker é necessário.

O envio externo FCM/APNs precisa ser validado em aparelhos configurados e com os arquivos/credenciais do proprietário. Não há credenciais externas neste repositório.
