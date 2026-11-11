# Serviço de e-mail

Os casos de uso dependem de `IEmailSender` no Core, sem SDK ou configuração de um fornecedor específico. A recuperação de senha compõe a mensagem em `PasswordRecoveryEmailDelivery` e usa a mesma interface. Outros módulos podem enviar mensagens pela interface sem alterar o transporte.

O adaptador SMTP usa MailKit, com conexão independente por envio, autenticação opcional, TLS/STARTTLS, timeout e cancelamento. O transporte não cria dependência de Brevo, Resend ou outro fornecedor. Trocar de serviço SMTP exige apenas configuração.

## Configurar SMTP

Defina estas variáveis no ambiente da API ou em seu gerenciador de segredos:

```text
Email__Provider=Smtp
Email__FromAddress=no-reply@seudominio.com
Email__FromName=Kiai Control
Email__ReplyToAddress=suporte@seudominio.com
Email__Smtp__Host=smtp.seuprovedor.com
Email__Smtp__Port=587
Email__Smtp__Security=StartTls
Email__Smtp__Username=<usuário SMTP>
Email__Smtp__Password=<senha, chave SMTP ou senha de aplicativo>
Email__Smtp__TimeoutSeconds=30
PasswordRecovery__ResetUrl=https://painel.seudominio.com/reset-password
```

Substitua os valores de exemplo pelos dados do provedor. Não versione credenciais. O remetente precisa ser aceito pelo provedor; autenticação de domínio/remetente é feita no serviço escolhido.

Reinicie a API após alterar o provedor ou suas opções; a configuração é validada e resolvida no processo da aplicação.

O arquivo `backend/kiai-control-api/src/KiaiControl.Api/appsettings.Email.example.json` documenta a estrutura equivalente. Ele não é carregado automaticamente: copie a seção desejada para a configuração apropriada ou use as variáveis acima. O envio começa desabilitado (`Provider=Disabled`) até a seleção e configuração de um adaptador. Não há um endpoint público de envio livre de e-mails.

| Modo `Email:Smtp:Security` | Uso |
| --- | --- |
| `StartTls` | Conexão com upgrade TLS obrigatório, normalmente na porta 587. Sem suporte do servidor, o envio falha; não há downgrade silencioso. |
| `SslOnConnect` | TLS desde o início da conexão, normalmente na porta 465. |
| `None` | Sem criptografia, para captura local ou relay de infraestrutura explicitamente confiável. |

A validação normal de certificados permanece ativa. Não há opção para aceitar qualquer certificado. Em produção, prefira TLS/STARTTLS conforme o provedor. Usuário e senha devem ser configurados juntos; deixe ambos vazios apenas para relay sem autenticação. Configuração inválida de um provedor habilitado impede o startup com mensagens que não expõem as credenciais.

Exemplos de servidores compatíveis com o adaptador SMTP:

| Serviço | Host | Porta / segurança | Credencial |
| --- | --- | --- | --- |
| Brevo | `smtp-relay.brevo.com` | `587` / `StartTls` | Login SMTP e chave SMTP do painel Brevo; não a chave da API HTTP. |
| Resend | `smtp.resend.com` | `587` / `StartTls` | Usuário `resend`; senha igual à chave de API. |
| SMTP próprio | Host do seu relay | Conforme a infraestrutura | Usuário/senha, ou ambos vazios se o relay permitir. |

Referências dos fornecedores: [SMTP Brevo](https://developers.brevo.com/docs/smtp-integration), [SMTP Resend](https://resend.com/docs/send-with-smtp). Estes exemplos não fixam o fornecedor no código. Provedores que exigem OAuth2 ou somente API HTTP podem usar outro adaptador, conforme a seção abaixo.

## Brevo no desenvolvimento local

A API tem o `UserSecretsId` `kiai-control-api-development`. Ao executar em `Development`, o ASP.NET Core carrega automaticamente os User Secrets depois dos arquivos `appsettings` e antes das variáveis de ambiente/argumentos. A configuração permanece fora do repositório; nenhum adaptador específico da Brevo é necessário.

No painel da Brevo:

1. Em **Configurações → SMTP e API → SMTP**, copie o **login SMTP** e gere uma **chave SMTP** para o Kiai Control. O login pode ser diferente do e-mail da sua conta. A chave SMTP é usada como senha; senha de acesso à Brevo e chave da API HTTP não servem para essa conexão.
2. Cadastre e verifique o endereço que será usado como remetente em **Remetentes e domínios**. Não use o login técnico `...@smtp-brevo.com` como remetente. Configure a autenticação do domínio conforme as instruções da Brevo.
3. Na raiz deste repositório, abra o arquivo de segredos da API em seu editor. No macOS/Linux ele fica em `~/.microsoft/usersecrets/kiai-control-api-development/secrets.json`; no Windows, em `%APPDATA%\Microsoft\UserSecrets\kiai-control-api-development\secrets.json`. Mescle estas seções, preservando eventuais outros segredos:

```json
{
  "Email": {
    "Provider": "Disabled",
    "FromAddress": "",
    "FromName": "Kiai Control",
    "Smtp": {
      "Host": "smtp-relay.brevo.com",
      "Port": 587,
      "Security": "StartTls",
      "Username": "",
      "Password": "",
      "TimeoutSeconds": 30
    }
  },
  "PasswordRecovery": {
    "ResetUrl": "http://localhost:5081/reset-password"
  }
}
```

4. Preencha `FromAddress` com o remetente verificado, `Username` com o login SMTP e `Password` com a chave SMTP. Depois altere `Provider` para `Smtp` e reinicie a API em `Development`. Enquanto os dados da conta não forem preenchidos, mantenha `Provider=Disabled`. Não coloque a chave em comandos de terminal que fiquem no histórico nem em arquivos versionados. User Secrets é armazenamento local de desenvolvimento, sem criptografia; em produção, use o gerenciador de segredos da infraestrutura e a URL HTTPS real do painel.

Configuração e limites confirmados em 2026-11-11: o plano gratuito oferece **300 envios por dia**. O Kiai Control não sincroniza contatos ou campanhas com a Brevo; usa o relay apenas para as mensagens transacionais solicitadas pelo sistema. O adaptador não consulta a cota nem implementa fila/retry próprio. Acompanhe aceitação, entrega e rejeições no painel transacional da Brevo.

Referências oficiais: [configurar o relay SMTP](https://help.brevo.com/hc/en-us/articles/7924908994450-Send-transactional-emails-using-Brevo-SMTP), [criar chaves SMTP](https://help.brevo.com/hc/en-us/articles/7959631848850-Create-and-manage-your-SMTP-keys), [validar login e remetente](https://help.brevo.com/hc/en-us/articles/115000188150-Troubleshooting-Issues-with-Brevo-SMTP), [limites do plano gratuito](https://help.brevo.com/hc/en-us/articles/208580669-FAQs-What-are-the-limits-of-the-Free-plan).

## Recuperação de senha

`PasswordRecovery:ResetUrl` é independente do transporte. Se não houver URL configurada ou o remetente estiver desabilitado, a recuperação por e-mail não fica habilitada. A API continua retornando a mensagem genérica, sem revelar existência de conta ou token; o painel permite redefinição administrativa.

A URL deve usar HTTPS. Somente em `Development`, HTTP é aceito para loopback (`localhost`, `127.0.0.1`, `::1`). O link mantém o token no fragmento `#token=...`, não em query string. O prazo e as regras de consumo/revogação da recuperação permanecem no módulo de autenticação.

## Captura local com Mailpit

Inicie o serviço de desenvolvimento:

```bash
docker compose -f compose.email.yml up -d
```

Configure o processo da API:

```text
ASPNETCORE_ENVIRONMENT=Development
Email__Provider=Smtp
Email__FromAddress=no-reply@kiai.test
Email__Smtp__Host=localhost
Email__Smtp__Port=1025
Email__Smtp__Security=None
Email__Smtp__Username=
Email__Smtp__Password=
PasswordRecovery__ResetUrl=http://localhost:5081/reset-password
```

Abra `http://localhost:8025` para consultar os e-mails capturados. O Mailpit recebe as mensagens localmente e não as encaminha para destinatários externos por padrão. As portas do Compose estão vinculadas ao loopback; as mensagens ficam em armazenamento temporário, com limite de 100 mensagens/24 horas. Encerre com:

```bash
docker compose -f compose.email.yml down
```

Referência: [imagem oficial do Mailpit](https://mailpit.axllent.org/docs/install/docker/).

## Usar em outro caso de uso

```csharp
public sealed class ExampleUseCase(IEmailSender emails)
{
    public Task<EmailSendResult> NotifyAsync(string address, CancellationToken ct) =>
        emails.SendAsync(new EmailMessage
        {
            To = new EmailAddress(address),
            Subject = "Aviso da academia",
            TextBody = "Seu cadastro foi atualizado.",
            HtmlBody = "<p>Seu cadastro foi atualizado.</p>"
        }, ct);
}
```

`EmailMessage` aceita destinatário com nome, assunto, texto e/ou HTML, além de remetente e Reply-To opcionais por mensagem. Quando omitidos, são usados os padrões da configuração. Cada mensagem tem um destinatário; envios coletivos podem ser compostos individualmente, preservando a privacidade dos endereços. Valores de usuários incluídos em templates HTML devem ser codificados pelo módulo que compõe o template.

O resultado distingue `Accepted`, `Disabled` e `Failed`. `Accepted` indica aceitação pelo transporte, não confirmação de entrega na caixa do destinatário. Falhas incluem categoria e indicação de transitoriedade, sem resposta bruta do servidor. Não existe retry automático, fila durável ou confirmação de entrega/bounce nesta implementação. Um módulo de fila/outbox ou webhooks de um fornecedor pode ser acrescentado sem mudar os casos de uso que dependem da interface.

Cancelamento solicitado pelo chamador é propagado. Falha de encerramento da sessão após aceitação SMTP não transforma o envio em falha, evitando sugerir reenvio de uma mensagem já aceita. Os logs do adaptador registram somente identificador da mensagem e estado/categoria, sem endereço do destinatário, assunto, corpo, token ou credenciais.

## Adicionar um fornecedor por API HTTP

Implemente `IEmailSender` em um adaptador de infraestrutura, retornando `EmailSendResult`. Registre-o depois de `AddKiaiApi` e antes de construir o host:

```csharp
builder.Services.AddEmailProvider<MyHttpEmailSender>("MyProvider");
```

Configure `Email__Provider=MyProvider` e `Email__FromAddress`. A fábrica seleciona o adaptador registrado, sem exigir configuração SMTP para esse fornecedor. O adaptador deve receber seu cliente HTTP/SDK e opções por DI e tratar autenticação, erros e credenciais na infraestrutura. A comparação do nome do fornecedor ignora maiúsculas/minúsculas. Nomes sem adaptador registrado falham na validação de startup.

Essa é a extensão para fornecedores sem SMTP ou com autenticação específica. As mensagens, os casos de uso e a recuperação de senha continuam dependentes apenas do contrato do Core.

## Compatibilidade com configuração anterior

Os campos antigos `Smtp:Host`, `Port`, `Username`, `Password`, `From` e `ResetUrl` continuam sendo lidos quando não há escolha explícita em `Email:Provider` nem host no novo formato. Porta 465 usa `SslOnConnect`; as demais usam `StartTls`. A URL nova de recuperação tem prioridade sobre `Smtp:ResetUrl`. Definir `Email:Provider=Disabled` desabilita o envio mesmo se houver credenciais antigas.

O `appsettings.json` traz apenas padrões sem credenciais, preservando essa migração. O envio implementado no produto continua sendo recuperação de senha; novos casos de negócio devem compor suas mensagens pela interface compartilhada.
