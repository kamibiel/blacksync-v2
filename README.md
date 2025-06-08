
# 🖥️ BlackSync (Avalonia Edition)

O **BlackSync** é uma aplicação desenvolvida para facilitar a migração de dados entre bancos de dados **Firebird (1.5 ou 2.1)** e **MySQL**, com foco em performance, controle de estrutura e logs detalhados. Essa versão foi recriada com **Avalonia UI** e **.NET 8.0**, tornando-se mais moderna e robusta para futuras evoluções.

---

## ✨ Funcionalidades Principais

- 🔁 Migração completa de tabelas do Firebird para MySQL  
- ⚙️ Verificação de estrutura com diagnóstico  
- 🧾 Geração automática de scripts de criação e ajuste  
- 💬 Geração de feedback automatizado em `.txt`  
- 📜 Log detalhado com barra de progresso  
- 🌐 Compatível com Windows  

---

## 📦 Requisitos

- [.NET 8.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- **Firebird Client** instalado (somente versões 1.5 ou 2.1)
- **MySQL Server** ou **MariaDB**
- **ODBC Drivers** para Firebird e MySQL
- Sistema Operacional: **Windows** (a versão atual ainda não é multiplataforma)

---

## 🔧 Instalação

### 👥 Para Usuários Finais

1️⃣ **Baixe o instalador**  
Acesse a página de [releases](https://github.com/seu-usuario/blacksync-v2/releases) e baixe a versão mais recente do `BlackSync_Setup.exe`.

2️⃣ **Execute o instalador**  
Siga as instruções da instalação e selecione os componentes adicionais necessários (como ODBC, Firebird Client, etc.).

3️⃣ **Configure as credenciais**  
Após a instalação, abra o programa e configure suas credenciais de acesso ao MySQL e Firebird.

4️⃣ **Pronto!** 🚀  
Agora você pode usar o BlackSync para migrar seus dados com segurança.

---

## 🛠️ Para Contribuidores (Desenvolvedores)

Se deseja contribuir com melhorias no BlackSync, siga os passos abaixo para configurar o ambiente de desenvolvimento.

### 1️⃣ Clone o Repositório

```bash
git clone https://github.com/seu-usuario/blacksync-v2.git
cd blacksync-v2
```

### 2️⃣ Configure as dependências

- .NET 8.0 SDK
- Visual Studio 2022, Rider ou VSCode
- Firebird ODBC Driver (v1.5 ou 2.1)
- MySQL Connector
- IBExpert (opcional)

### 3️⃣ Compile e execute

Abra o arquivo `BlackSync.sln` e execute a aplicação.

### 4️⃣ Sugira melhorias

Crie uma branch, realize suas alterações e envie um Pull Request.

---

## 📂 Estrutura do Projeto

```
📁 Views/         -> Telas da interface
📁 Services/      -> Lógica de migração, script, mensagens
📁 Models/        -> Tipos de dados e estruturas
📁 Resources/     -> Ícones e arquivos auxiliares
📄 README.md      -> Este documento
```

---

## 🧠 Autor

Desenvolvido por **Gabriel Bonifácio**  
📧 contato: gabriel.o.bonifacio@gmail.com

---

## 📃 Licença

Este projeto está licenciado sob os termos da **MIT License**.
