local item = {}

item.name = "CorreWithCare/ProgressBarToggle"

item.placements = {
	name = "normal",
	data = {
		mode = 1,
		target = "default",
		isCounter = true,
	},
}

item.fieldInformation = {
	mode = {
		options = {
			["Appear"] = 1,
			["Disappear"] = 2,
		},
		editable = false,
	}
}

return item