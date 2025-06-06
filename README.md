# 🖥️ BlackSync v2.0.0 (Avalonia UI)

🚀 **BlackSync** é uma aplicação moderna para sincronização de dados entre bancos **Firebird** e **MySQL**, desenvolvida com Avalonia UI — multiplataforma, leve e com visual refinado.

> ⚠️ Esta é a **nova versão oficial**. A versão anterior em Windows Forms (v1.2.0) ainda está disponível [aqui](https://github.com/kamibiel/blacksync).

---

## ✨ Funcionalidades Principais

- 🔁 Migração completa de tabelas do Firebird para MySQL
- ⚙️ Verificação de estrutura com diagnóstico
- 🧾 Geração automática de scripts de criação e ajuste
- 💬 Feedback automatizado em arquivo `.txt`
- 📜 Log detalhado e barra de progresso
- 🌐 Compatível com Windows, Linux e macOS

---

## 📦 Requisitos

- .NET 8.0 ou superior
- Firebird Client instalado (v2.1+)
- MySQL Server ou MariaDB
- DSN configurado para acesso ao Firebird

---

## ▶️ Como Usar

1. Clone o repositório:
   ```bash
   git clone https://github.com/kamibiel/blacksync-v2.git
   ```
2. Compile com `dotnet build` ou abra no Rider/Visual Studio
3. Execute a aplicação
4. Configure os acessos aos bancos
5. Selecione as tabelas e migre!

---

## 📂 Estrutura do Projeto

- `Views/` – Telas da interface
- `Services/` – Lógica de migração, geração de script e mensagens
- `Models/` – Tipos de dados
- `Resources/` – Ícones e arquivos auxiliares
- `README.md` – Este documento

---

## 🤝 Contribuição

Contribuições são bem-vindas! Envie um PR ou abra uma issue com sugestões e melhorias.

---

## 🧠 Autor

Desenvolvido por **Gabriel Bonifácio**  
📧 contato: [gabriel.bonifacio@gmail.com](mailto:gabriel.bonifacio@gmail.com)

---

## 📃 Licença

Este projeto está licenciado sob a [MIT License](LICENSE).
