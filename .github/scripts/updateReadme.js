const fs = require("fs");
const axios = require("axios");

const REPO_OWNER = "ilikomanov";
const REPO_NAME = "DatingApp-net8";

const README_PATH = "README.md";

async function updateReadme() {
  const res = await axios.get(`https://api.github.com/repos/${REPO_OWNER}/${REPO_NAME}/languages`);
  const data = res.data;

  const total = Object.values(data).reduce((a, b) => a + b, 0);
  const percentages = Object.entries(data)
    .map(([lang, bytes]) => {
      const emoji = getEmoji(lang);
      return `- ${emoji} **${lang}** – ${(bytes / total * 100).toFixed(1)}%`;
    })
    .join('\n');

  const readme = fs.readFileSync(README_PATH, 'utf8');

  const newReadme = readme.replace(
    /### 📊 Language Usage in `DatingApp-net8`[\s\S]*?(?=\n---)/,
    `### 📊 Language Usage in \`DatingApp-net8\`\n\n${percentages}`
  );

  fs.writeFileSync(README_PATH, newReadme);
}

function getEmoji(lang) {
  switch (lang.toLowerCase()) {
    case "c#": return "🟢";
    case "typescript": return "🔵";
    case "html": return "🔴";
    case "css": return "🟠";
    case "dockerfile": return "🟡";
    default: return "⚪";
  }
}

updateReadme().catch((err) => {
  console.error("Failed to update README:", err.message);
  process.exit(1);
});
