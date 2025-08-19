const fs = require("fs");
const axios = require("axios");

const REPO_OWNER = "ilikomanov";
const REPO_NAME = "DatingApp-net8";
const README_PATH = "README.md";
const SECTION_TITLE = "### 📊 Language Usage in `DatingApp-net8`";

async function updateReadme() {
  try {
    const res = await axios.get(`https://api.github.com/repos/${REPO_OWNER}/${REPO_NAME}/languages`);
    const data = res.data;

    const total = Object.values(data).reduce((a, b) => a + b, 0);
    const percentages = Object.entries(data)
      .map(([lang, bytes]) => {
        const emoji = getEmoji(lang);
        const percent = (bytes / total * 100).toFixed(1);
        return `- ${emoji} **${lang}** – ${percent}%`;
      })
      .join('\n');

    const readme = fs.readFileSync(README_PATH, 'utf8');

    // Create updated language block
    const updatedSection = `${SECTION_TITLE}\n\n${percentages}`;
    const regex = new RegExp(`${SECTION_TITLE}[\\s\\S]*?(?=\\n---)`, 'm');

    if (!regex.test(readme)) {
      console.error("❌ Could not find the target section in README.md.");
      process.exit(1);
    }

    const newReadme = readme.replace(regex, updatedSection);

    if (newReadme === readme) {
      console.log("✅ No changes to README.md — skipping write.");
      return;
    }

    fs.writeFileSync(README_PATH, newReadme);
    console.log("✅ README.md updated successfully.");
  } catch (err) {
    console.error("❌ Failed to update README:", err.message);
    process.exit(1);
  }
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

updateReadme();
