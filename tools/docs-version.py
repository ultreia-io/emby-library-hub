"""Use the assembly release version in every documentation page footer."""
from pathlib import Path
import xml.etree.ElementTree as ET


def on_config(config):
    root = Path(config.config_file_path).resolve().parent
    version = ET.parse(root / "Directory.Build.props").findtext("./PropertyGroup/Version")
    if not version:
        raise ValueError("Directory.Build.props must define Version.")
    config.copyright = config.copyright.replace("{{VERSION}}", version)
    return config
